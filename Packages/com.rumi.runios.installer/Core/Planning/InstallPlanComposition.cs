#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Planning
{
    static class InstallPlanComposition
    {
        internal static PackageResult<InstallPlan> Compose(PlanningRequest request, IReadOnlyList<PackageChange> changes,
            PlanFragmentSet fragments, List<PackageDiagnostic> diagnostics)
        {
            Dictionary<string, PlannedOperation> operations = new(StringComparer.Ordinal);
            Dictionary<(PackageId, PackageOperationPhase), PackageOperationBoundary> boundaries = new();
            foreach (PlanFragment fragment in fragments.fragments)
            {
                foreach (PlannedOperation operation in fragment.operations)
                {
                    if (!operations.TryAdd(operation.id, operation))
                        diagnostics.Add(InstallPlanner.Error("duplicate-operation", $"Operation '{operation.id}' is duplicated."));
                }
                foreach (PackageOperationBoundary boundary in fragment.boundaries)
                {
                    if (!boundaries.TryAdd((boundary.packageId, boundary.phase), boundary))
                        diagnostics.Add(InstallPlanner.Error("duplicate-package-boundary", $"Package '{boundary.packageId}' has duplicate '{boundary.phase}' boundaries.", boundary.packageId));
                }
            }

            HashSet<(PackageId, PackageOperationPhase)> required = new();
            foreach (PackageChange change in changes)
            {
                if (change.desiredInstallation is not null)
                    required.Add((change.id, PackageOperationPhase.Install));
                if (change.current is not null && (change.desiredInstallation is null ||
                    !Snapshots.SameInstallation(change.current.installation, change.desiredInstallation)))
                    required.Add((change.id, PackageOperationPhase.Remove));
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
                    diagnostics.Add(InstallPlanner.Error("unexpected-package-boundary", $"Package '{boundary.packageId}' has an unrequested '{boundary.phase}' boundary.", boundary.packageId));
                if (boundary.startOperations.Count == 0 || boundary.completionOperations.Count == 0)
                    diagnostics.Add(InstallPlanner.Error("empty-package-boundary", $"Package '{boundary.packageId}' has an empty operation boundary.", boundary.packageId));
                foreach (string id in boundary.startOperations.Concat(boundary.completionOperations))
                {
                    if (!operations.ContainsKey(id))
                        diagnostics.Add(InstallPlanner.Error("missing-boundary-operation", $"Boundary operation '{id}' does not exist.", boundary.packageId));
                }
            }
            foreach (PlannedOperation operation in operations.Values)
            {
                foreach (string prerequisite in operation.prerequisites)
                {
                    if (!operations.ContainsKey(prerequisite))
                        diagnostics.Add(InstallPlanner.Error("missing-operation-prerequisite", $"Operation '{operation.id}' requires missing operation '{prerequisite}'."));
                    if (operation.id == prerequisite)
                        diagnostics.Add(InstallPlanner.Error("operation-self-dependency", $"Operation '{operation.id}' requires itself."));
                }
            }
            if (InstallPlanner.HasErrors(diagnostics))
                return new PackageResult<InstallPlan>(null, diagnostics);

            Dictionary<string, HashSet<string>> prerequisites = operations.ToDictionary(x => x.Key,
                x => new HashSet<string>(x.Value.prerequisites, StringComparer.Ordinal), StringComparer.Ordinal);

            void Order(PackageOperationBoundary before, PackageOperationBoundary after)
            {
                foreach (string start in after.startOperations)
                {
                    foreach (string completion in before.completionOperations)
                    {
                        // A shared atomic operation can establish multiple package states at once.
                        if (start != completion)
                            prerequisites[start].Add(completion);
                    }
                }
            }

            foreach (ResolvedDependency dependency in request.desired.dependencies)
            {
                if (boundaries.TryGetValue((dependency.dependency, PackageOperationPhase.Install), out PackageOperationBoundary? before) && before is not null &&
                    boundaries.TryGetValue((dependency.owner, PackageOperationPhase.Install), out PackageOperationBoundary? after) && after is not null)
                    Order(before, after);
            }
            foreach (InstalledPackage package in request.current.packages.packages.Values)
            {
                foreach (PackageId dependency in package.dependencies)
                {
                    if (!boundaries.TryGetValue((dependency, PackageOperationPhase.Remove), out PackageOperationBoundary? after) || after is null)
                        continue;
                    if (boundaries.TryGetValue((package.id, PackageOperationPhase.Remove), out PackageOperationBoundary? before) && before is not null)
                        Order(before, after);
                    else if (boundaries.TryGetValue((package.id, PackageOperationPhase.Install), out before) && before is not null &&
                        request.desired.packages.TryGetValue(package.id, out ResolvedPackage? desired) && desired is not null &&
                        !desired.metadata.dependencies.Any(x => x.requirement.id == dependency))
                        Order(before, after);
                }
            }
            foreach (PackageChange change in changes)
            {
                if (boundaries.TryGetValue((change.id, PackageOperationPhase.Remove), out PackageOperationBoundary? before) && before is not null &&
                    boundaries.TryGetValue((change.id, PackageOperationPhase.Install), out PackageOperationBoundary? after) && after is not null)
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
                if (boundary.completionOperations.Any(completion => !boundary.startOperations.Any(start => Connected(completion, start))) ||
                    boundary.startOperations.Any(start => !boundary.completionOperations.Any(completion => Connected(completion, start))))
                    diagnostics.Add(InstallPlanner.Error("disconnected-package-boundary", $"Package '{boundary.packageId}' has disconnected start and completion operations.", boundary.packageId));
            }

            Dictionary<string, Dictionary<string, ResourceAccess>> resources = new(StringComparer.Ordinal);
            foreach (PlannedOperation operation in operations.Values)
            {
                foreach (ResourceClaim claim in operation.claims)
                {
                    if (!resources.TryGetValue(claim.resource, out Dictionary<string, ResourceAccess>? users) || users is null)
                    {
                        users = new Dictionary<string, ResourceAccess>(StringComparer.Ordinal);
                        resources.Add(claim.resource, users);
                    }
                    if (!users.ContainsKey(operation.id) || claim.access == ResourceAccess.Write)
                        users[operation.id] = claim.access;
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
                new PlannedOperation(id, operations[id].operation, Dependencies(id), operations[id].claims))), diagnostics);
        }
    }
}
