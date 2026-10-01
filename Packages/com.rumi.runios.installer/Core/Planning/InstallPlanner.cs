#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;

namespace RuniOS.PackageManagement.Planning
{
    /// <summary>
    /// Compares desired state with observations and composes registered installation plans.<br/>
    /// 원하는 상태와 관측을 비교하고 등록된 설치 계획을 합성합니다.
    /// </summary>
    public sealed class InstallPlanner
    {
        readonly PackageManagementServices services;

        internal InstallPlanner(PackageManagementServices services) => this.services = services;

        /// <summary>
        /// Produces a passive plan without acquisition, file access, or operation execution.<br/>
        /// 콘텐츠 획득, 파일 접근 또는 작업 실행 없이 수동적 계획을 생성합니다.
        /// </summary>
        /// <param name="request">
        /// Immutable desired state, observations, bindings, and management policy.<br/>
        /// 불변 원하는 상태, 관측, 바인딩 및 관리 정책입니다.
        /// </param>
        /// <returns>
        /// A validated plan or diagnostics without a plan.<br/>
        /// 검증된 계획을 반환하거나 계획 없이 진단을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="request"/> is <see langword="null"/>.<br/>
        /// <paramref name="request"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public PackageResult<InstallPlan> Plan(PlanningRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));
            List<PackageDiagnostic> diagnostics = new();
            if (!request.current.packages.isComplete)
                diagnostics.Add(Error("incomplete-installed-state", "Installed-state discovery is incomplete."));

            List<PackageChange> changes = new();
            foreach (KeyValuePair<PackageId, ResolvedPackage> desired in request.desired.packages.OrderBy(x => x.Key.value, StringComparer.Ordinal))
            {
                if (!request.installations.TryGetValue(desired.Key, out PackageInstallation? installation) || installation is null)
                {
                    diagnostics.Add(Error("missing-installation-binding", $"No installation is bound to '{desired.Key}'.", desired.Key));
                    continue;
                }
                request.current.packages.packages.TryGetValue(desired.Key, out InstalledPackage? current);
                if (current is null)
                {
                    changes.Add(new PackageChange(desired.Key, PackageChangeKind.Install, null, desired.Value, installation));
                    continue;
                }
                PackageIdentity? currentIdentity = current.identity;
                if (currentIdentity is null)
                {
                    diagnostics.Add(Error("unknown-installed-identity", $"The installed revision of '{desired.Key}' is unknown.", desired.Key));
                    continue;
                }
                bool sameInstallation = Snapshots.SameInstallation(current.installation, installation);
                if (currentIdentity.Equals(desired.Value.identity) && sameInstallation)
                    continue;
                if (current.managementOwner != request.managementOwner && !request.allowAdoption)
                {
                    diagnostics.Add(Error("unowned-package-change", $"Changing '{desired.Key}' requires explicit adoption permission.", desired.Key));
                    continue;
                }
                changes.Add(new PackageChange(desired.Key, sameInstallation ? PackageChangeKind.Update : PackageChangeKind.ChangeInstallation,
                    current, desired.Value, installation));
            }
            foreach (PackageId id in request.installations.Keys.Where(x => !request.desired.packages.ContainsKey(x)))
                diagnostics.Add(Error("unused-installation-binding", $"Installation binding '{id}' has no desired package.", id));

            if (request.removeUnusedOwnedPackages)
            {
                foreach (InstalledPackage package in request.current.packages.packages.Values.OrderBy(x => x.id.value, StringComparer.Ordinal))
                {
                    if (package.managementOwner == request.managementOwner && !request.desired.packages.ContainsKey(package.id))
                        changes.Add(new PackageChange(package.id, PackageChangeKind.Remove, package, null, null));
                }
            }
            HashSet<PackageId> removals = new(changes.Where(x => x.kind == PackageChangeKind.Remove).Select(x => x.id));
            foreach (InstalledPackage package in request.current.packages.packages.Values)
            {
                if (removals.Contains(package.id))
                    continue;
                IEnumerable<PackageId> retainedDependencies = request.desired.packages.TryGetValue(package.id, out ResolvedPackage? retained) && retained is not null
                    ? retained.metadata.dependencies.Select(x => x.requirement.id) : package.dependencies;
                foreach (PackageId dependency in retainedDependencies.Where(removals.Contains))
                    diagnostics.Add(Error("package-still-required", $"'{package.id}' still requires removal candidate '{dependency}'.", dependency));

                if (request.desired.packages.ContainsKey(package.id))
                    continue;
                foreach (PackageId dependency in package.dependencies)
                {
                    if (!request.desired.packages.TryGetValue(dependency, out ResolvedPackage? desiredDependency) || desiredDependency is null)
                        continue;
                    if (request.current.packages.packages.TryGetValue(dependency, out InstalledPackage? installedDependency) && installedDependency is not null &&
                        desiredDependency.identity.Equals(installedDependency.identity))
                        continue;
                    PackageRequirement[] declarations = package.dependencyRequirements.Where(x => x.id == dependency).ToArray();
                    if (declarations.Length == 0)
                    {
                        diagnostics.Add(Error("unknown-retained-requirement", $"Compatibility of retained package '{package.id}' with changed dependency '{dependency}' is unknown.", dependency));
                        continue;
                    }
                    PackageCandidate candidate = new(desiredDependency.identity, desiredDependency.artifacts);
                    foreach (PackageConstraint constraint in declarations.SelectMany(x => x.constraints))
                    {
                        if (!services.evaluators.TryGetValue(constraint.kind, out IPackageConstraintEvaluator? evaluator) || evaluator is null)
                        {
                            diagnostics.Add(Error("unsupported-retained-constraint", $"No evaluator verifies '{constraint.kind}' required by retained package '{package.id}'.", dependency));
                            continue;
                        }
                        ConstraintMatch match = evaluator.Evaluate(constraint, candidate);
                        diagnostics.AddRange(match.diagnostics.Select(x => new PackageDiagnostic(x.code, x.severity,
                            PackageDiagnosticPhase.Planning, x.message, x.dependencyPath, x.locations)));
                        if (!match.isMatch)
                            diagnostics.Add(Error("retained-requirement-conflict", $"Changed dependency '{dependency}' does not satisfy retained package '{package.id}'.", dependency));
                    }
                }
            }
            if (HasErrors(diagnostics))
                return new PackageResult<InstallPlan>(null, diagnostics);

            string[] kinds = changes.SelectMany(x => new[] { x.current?.installation.kind, x.desiredInstallation?.kind })
                .Concat(request.installations.Values.Select(x => x.kind))
                .OfType<string>().Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            List<PlanFragment> fragments = new();
            foreach (string kind in kinds)
            {
                if (!services.planners.TryGetValue(kind, out IInstallationPlanningProvider? provider) || provider is null)
                {
                    diagnostics.Add(Error("no-installation-provider", $"No planning provider handles '{kind}'."));
                    continue;
                }
                InstallationPlanningContext context = new(request, kind, changes);
                PackageResult<PlanFragment> result = provider.Plan(context);
                diagnostics.AddRange(result.diagnostics);
                PlanFragment? fragment = result.value;
                if (result.succeeded && fragment is not null)
                    fragments.Add(fragment);
                else if (!HasErrors(result.diagnostics))
                    diagnostics.Add(Error("missing-planning-result", $"Planning provider '{kind}' returned no fragment."));
            }
            if (HasErrors(diagnostics))
                return new PackageResult<InstallPlan>(null, diagnostics);

            PlanFragmentSet fragmentSet = new(fragments);
            foreach (IPlanFragmentComposer composer in services.composers)
            {
                PackageResult<PlanFragmentSet> result = composer.Compose(request, fragmentSet);
                diagnostics.AddRange(result.diagnostics);
                PlanFragmentSet? composed = result.value;
                if (!result.succeeded || composed is null)
                {
                    if (!HasErrors(result.diagnostics))
                        diagnostics.Add(Error("missing-composition-result", $"Composer '{composer.key}' returned no fragments."));
                    return new PackageResult<InstallPlan>(null, diagnostics);
                }
                fragmentSet = composed;
            }
            return InstallPlanComposition.Compose(request, changes, fragmentSet, diagnostics);
        }

        internal static bool HasErrors(IEnumerable<PackageDiagnostic> diagnostics) =>
            diagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error);

        internal static PackageDiagnostic Error(string code, string message, PackageId? id = null) =>
            PackageDiagnostic.Error("package:" + code, PackageDiagnosticPhase.Planning, message, id.HasValue ? new[] { id.Value } : null);
    }
}
