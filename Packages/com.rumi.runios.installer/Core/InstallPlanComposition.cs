#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
{
    static class InstallPlanComposition
    {
        internal static PackageResult<InstallPlan> Compose(PlanningRequest request, IReadOnlyList<PackageChange> changes,
            PlanFragmentSet fragments, List<PackageDiagnostic> diagnostics)
        {
            Dictionary<string, PlannedOperation> operations = new(StringComparer.Ordinal);
            Dictionary<(PackageId, PackageOperationPhase), PackageOperationBoundary> boundaries = new();
            foreach (PlanFragment fragment in fragments.Fragments)
            {
                foreach (PlannedOperation operation in fragment.Operations)
                {
                    if (!operations.TryAdd(operation.Id, operation))
                        diagnostics.Add(InstallPlanner.Error("duplicate-operation", $"Operation '{operation.Id}' is duplicated."));
                }
                foreach (PackageOperationBoundary boundary in fragment.Boundaries)
                {
                    if (!boundaries.TryAdd((boundary.PackageId, boundary.Phase), boundary))
                        diagnostics.Add(InstallPlanner.Error("duplicate-package-boundary", $"Package '{boundary.PackageId}' has duplicate '{boundary.Phase}' boundaries.", boundary.PackageId));
                }
            }

            HashSet<(PackageId, PackageOperationPhase)> required = new();
            foreach (PackageChange change in changes)
            {
                if (change.DesiredInstallation is not null)
                    required.Add((change.Id, PackageOperationPhase.Install));
                if (change.Current is not null && (change.DesiredInstallation is null ||
                    !Snapshots.SameInstallation(change.Current.Installation, change.DesiredInstallation)))
                    required.Add((change.Id, PackageOperationPhase.Remove));
            }
            foreach ((PackageId id, PackageOperationPhase phase) in required)
            {
                if (!boundaries.ContainsKey((id, phase)))
                    diagnostics.Add(InstallPlanner.Error("missing-package-boundary", $"No '{phase}' operation boundary implements '{id}'.", id));
            }
            foreach (KeyValuePair<(PackageId, PackageOperationPhase), PackageOperationBoundary> entry in boundaries)
            {
                PackageOperationBoundary boundary = entry.Value;
                if (!required.Contains(entry.Key))
                    diagnostics.Add(InstallPlanner.Error("unexpected-package-boundary", $"Package '{boundary.PackageId}' has an unrequested '{boundary.Phase}' boundary.", boundary.PackageId));
                if (boundary.StartOperations.Count == 0 || boundary.CompletionOperations.Count == 0)
                    diagnostics.Add(InstallPlanner.Error("empty-package-boundary", $"Package '{boundary.PackageId}' has an empty operation boundary.", boundary.PackageId));
                foreach (string id in boundary.StartOperations.Concat(boundary.CompletionOperations))
                {
                    if (!operations.ContainsKey(id))
                        diagnostics.Add(InstallPlanner.Error("missing-boundary-operation", $"Boundary operation '{id}' does not exist.", boundary.PackageId));
                }
            }
            foreach (PlannedOperation operation in operations.Values)
            {
                foreach (string prerequisite in operation.Prerequisites)
                {
                    if (!operations.ContainsKey(prerequisite))
                        diagnostics.Add(InstallPlanner.Error("missing-operation-prerequisite", $"Operation '{operation.Id}' requires missing operation '{prerequisite}'."));
                    if (operation.Id == prerequisite)
                        diagnostics.Add(InstallPlanner.Error("operation-self-dependency", $"Operation '{operation.Id}' requires itself."));
                }
            }
            if (InstallPlanner.HasErrors(diagnostics))
                return new PackageResult<InstallPlan>(null, diagnostics);

            Dictionary<string, HashSet<string>> prerequisites = operations.ToDictionary(x => x.Key,
                x => new HashSet<string>(x.Value.Prerequisites, StringComparer.Ordinal), StringComparer.Ordinal);

            void Order(PackageOperationBoundary before, PackageOperationBoundary after)
            {
                foreach (string start in after.StartOperations)
                {
                    foreach (string completion in before.CompletionOperations)
                    {
                        // A shared atomic operation can establish multiple package states at once.
                        if (start != completion)
                            prerequisites[start].Add(completion);
                    }
                }
            }

            foreach (ResolvedDependency dependency in request.Desired.Dependencies)
            {
                if (boundaries.TryGetValue((dependency.Dependency, PackageOperationPhase.Install), out PackageOperationBoundary? before) && before is not null &&
                    boundaries.TryGetValue((dependency.Owner, PackageOperationPhase.Install), out PackageOperationBoundary? after) && after is not null)
                    Order(before, after);
            }
            foreach (InstalledPackage package in request.Current.Packages.Packages.Values)
            {
                foreach (PackageId dependency in package.Dependencies)
                {
                    if (!boundaries.TryGetValue((dependency, PackageOperationPhase.Remove), out PackageOperationBoundary? after) || after is null)
                        continue;
                    if (boundaries.TryGetValue((package.Id, PackageOperationPhase.Remove), out PackageOperationBoundary? before) && before is not null)
                        Order(before, after);
                    else if (boundaries.TryGetValue((package.Id, PackageOperationPhase.Install), out before) && before is not null &&
                        request.Desired.Packages.TryGetValue(package.Id, out ResolvedPackage? desired) && desired is not null &&
                        !desired.Metadata.Dependencies.Any(x => x.Requirement.Id == dependency))
                        Order(before, after);
                }
            }
            foreach (PackageChange change in changes)
            {
                if (boundaries.TryGetValue((change.Id, PackageOperationPhase.Remove), out PackageOperationBoundary? before) && before is not null &&
                    boundaries.TryGetValue((change.Id, PackageOperationPhase.Install), out PackageOperationBoundary? after) && after is not null)
                    Order(before, after);
            }

            IEnumerable<string> Dependencies(string id) => prerequisites[id].OrderBy(x => x, StringComparer.Ordinal);
            if (!GraphOrder.TryOrder(operations.Keys.OrderBy(x => x, StringComparer.Ordinal), Dependencies,
                out IReadOnlyList<string> order, out IReadOnlyList<string> cycle))
            {
                diagnostics.Add(InstallPlanner.Error("operation-cycle", $"Composed operation prerequisites contain a cycle: {string.Join(", ", cycle)}."));
                return new PackageResult<InstallPlan>(null, diagnostics);
            }

            foreach (PackageOperationBoundary boundary in boundaries.Values)
            {
                bool Connected(string completion, string start) => completion == start || GraphOrder.DependsOn(completion, start, Dependencies);
                if (boundary.CompletionOperations.Any(completion => !boundary.StartOperations.Any(start => Connected(completion, start))) ||
                    boundary.StartOperations.Any(start => !boundary.CompletionOperations.Any(completion => Connected(completion, start))))
                    diagnostics.Add(InstallPlanner.Error("disconnected-package-boundary", $"Package '{boundary.PackageId}' has disconnected start and completion operations.", boundary.PackageId));
            }

            Dictionary<string, Dictionary<string, ResourceAccess>> resources = new(StringComparer.Ordinal);
            foreach (PlannedOperation operation in operations.Values)
            {
                foreach (ResourceClaim claim in operation.Claims)
                {
                    if (!resources.TryGetValue(claim.Resource, out Dictionary<string, ResourceAccess>? users) || users is null)
                    {
                        users = new Dictionary<string, ResourceAccess>(StringComparer.Ordinal);
                        resources.Add(claim.Resource, users);
                    }
                    if (!users.ContainsKey(operation.Id) || claim.Access == ResourceAccess.Write)
                        users[operation.Id] = claim.Access;
                }
            }
            foreach (KeyValuePair<string, Dictionary<string, ResourceAccess>> resource in resources)
            {
                KeyValuePair<string, ResourceAccess>[] users = resource.Value.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
                for (int i = 0; i < users.Length; i++)
                {
                    for (int j = i + 1; j < users.Length; j++)
                    {
                        if (users[i].Value == ResourceAccess.Read && users[j].Value == ResourceAccess.Read)
                            continue;
                        if (!GraphOrder.DependsOn(users[i].Key, users[j].Key, Dependencies) &&
                            !GraphOrder.DependsOn(users[j].Key, users[i].Key, Dependencies))
                            diagnostics.Add(InstallPlanner.Error("resource-conflict", $"Unordered operations '{users[i].Key}' and '{users[j].Key}' conflict on '{resource.Key}'."));
                    }
                }
            }
            if (InstallPlanner.HasErrors(diagnostics))
                return new PackageResult<InstallPlan>(null, diagnostics);

            return new PackageResult<InstallPlan>(new InstallPlan(request, changes, order.Select(id =>
                new PlannedOperation(id, operations[id].Operation, Dependencies(id), operations[id].Claims))), diagnostics);
        }
    }
}
