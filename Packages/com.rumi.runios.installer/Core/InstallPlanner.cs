#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
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
            if (!request.Current.Packages.IsComplete)
                diagnostics.Add(Error("incomplete-installed-state", "Installed-state discovery is incomplete."));

            List<PackageChange> changes = new();
            foreach (KeyValuePair<PackageId, ResolvedPackage> desired in request.Desired.Packages.OrderBy(x => x.Key.Value, StringComparer.Ordinal))
            {
                if (!request.Installations.TryGetValue(desired.Key, out PackageInstallation? installation) || installation is null)
                {
                    diagnostics.Add(Error("missing-installation-binding", $"No installation is bound to '{desired.Key}'.", desired.Key));
                    continue;
                }
                request.Current.Packages.Packages.TryGetValue(desired.Key, out InstalledPackage? current);
                if (current is null)
                {
                    changes.Add(new PackageChange(desired.Key, PackageChangeKind.Install, null, desired.Value, installation));
                    continue;
                }
                PackageIdentity? currentIdentity = current.Identity;
                if (currentIdentity is null)
                {
                    diagnostics.Add(Error("unknown-installed-identity", $"The installed revision of '{desired.Key}' is unknown.", desired.Key));
                    continue;
                }
                bool sameInstallation = Snapshots.SameInstallation(current.Installation, installation);
                if (currentIdentity.Equals(desired.Value.Identity) && sameInstallation)
                    continue;
                if (current.ManagementOwner != request.ManagementOwner && !request.AllowAdoption)
                {
                    diagnostics.Add(Error("unowned-package-change", $"Changing '{desired.Key}' requires explicit adoption permission.", desired.Key));
                    continue;
                }
                changes.Add(new PackageChange(desired.Key, sameInstallation ? PackageChangeKind.Update : PackageChangeKind.ChangeInstallation,
                    current, desired.Value, installation));
            }
            foreach (PackageId id in request.Installations.Keys.Where(x => !request.Desired.Packages.ContainsKey(x)))
                diagnostics.Add(Error("unused-installation-binding", $"Installation binding '{id}' has no desired package.", id));

            if (request.RemoveUnusedOwnedPackages)
            {
                foreach (InstalledPackage package in request.Current.Packages.Packages.Values.OrderBy(x => x.Id.Value, StringComparer.Ordinal))
                {
                    if (package.ManagementOwner == request.ManagementOwner && !request.Desired.Packages.ContainsKey(package.Id))
                        changes.Add(new PackageChange(package.Id, PackageChangeKind.Remove, package, null, null));
                }
            }
            HashSet<PackageId> removals = new(changes.Where(x => x.Kind == PackageChangeKind.Remove).Select(x => x.Id));
            foreach (InstalledPackage package in request.Current.Packages.Packages.Values)
            {
                if (removals.Contains(package.Id))
                    continue;
                IEnumerable<PackageId> retainedDependencies = request.Desired.Packages.TryGetValue(package.Id, out ResolvedPackage? retained) && retained is not null
                    ? retained.Metadata.Dependencies.Select(x => x.Requirement.Id) : package.Dependencies;
                foreach (PackageId dependency in retainedDependencies.Where(removals.Contains))
                    diagnostics.Add(Error("package-still-required", $"'{package.Id}' still requires removal candidate '{dependency}'.", dependency));

                if (request.Desired.Packages.ContainsKey(package.Id))
                    continue;
                foreach (PackageId dependency in package.Dependencies)
                {
                    if (!request.Desired.Packages.TryGetValue(dependency, out ResolvedPackage? desiredDependency) || desiredDependency is null)
                        continue;
                    if (request.Current.Packages.Packages.TryGetValue(dependency, out InstalledPackage? installedDependency) && installedDependency is not null &&
                        desiredDependency.Identity.Equals(installedDependency.Identity))
                        continue;
                    PackageRequirement[] declarations = package.DependencyRequirements.Where(x => x.Id == dependency).ToArray();
                    if (declarations.Length == 0)
                    {
                        diagnostics.Add(Error("unknown-retained-requirement", $"Compatibility of retained package '{package.Id}' with changed dependency '{dependency}' is unknown.", dependency));
                        continue;
                    }
                    PackageCandidate candidate = new(desiredDependency.Identity, desiredDependency.Artifacts);
                    foreach (PackageConstraint constraint in declarations.SelectMany(x => x.Constraints))
                    {
                        if (!services.Evaluators.TryGetValue(constraint.Kind, out IPackageConstraintEvaluator? evaluator) || evaluator is null)
                        {
                            diagnostics.Add(Error("unsupported-retained-constraint", $"No evaluator verifies '{constraint.Kind}' required by retained package '{package.Id}'.", dependency));
                            continue;
                        }
                        ConstraintMatch match = evaluator.Evaluate(constraint, candidate);
                        diagnostics.AddRange(match.Diagnostics.Select(x => new PackageDiagnostic(x.Code, x.Severity,
                            PackageDiagnosticPhase.Planning, x.Message, x.DependencyPath, x.Locations)));
                        if (!match.IsMatch)
                            diagnostics.Add(Error("retained-requirement-conflict", $"Changed dependency '{dependency}' does not satisfy retained package '{package.Id}'.", dependency));
                    }
                }
            }
            if (HasErrors(diagnostics))
                return new PackageResult<InstallPlan>(null, diagnostics);

            string[] kinds = changes.SelectMany(x => new[] { x.Current?.Installation.Kind, x.DesiredInstallation?.Kind })
                .Concat(request.Installations.Values.Select(x => x.Kind))
                .OfType<string>().Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
            List<PlanFragment> fragments = new();
            foreach (string kind in kinds)
            {
                if (!services.Planners.TryGetValue(kind, out IInstallationPlanningProvider? provider) || provider is null)
                {
                    diagnostics.Add(Error("no-installation-provider", $"No planning provider handles '{kind}'."));
                    continue;
                }
                InstallationPlanningContext context = new(request, kind, changes);
                PackageResult<PlanFragment> result = provider.Plan(context);
                diagnostics.AddRange(result.Diagnostics);
                PlanFragment? fragment = result.Value;
                if (result.Succeeded && fragment is not null)
                    fragments.Add(fragment);
                else if (!HasErrors(result.Diagnostics))
                    diagnostics.Add(Error("missing-planning-result", $"Planning provider '{kind}' returned no fragment."));
            }
            if (HasErrors(diagnostics))
                return new PackageResult<InstallPlan>(null, diagnostics);

            PlanFragmentSet fragmentSet = new(fragments);
            foreach (IPlanFragmentComposer composer in services.Composers)
            {
                PackageResult<PlanFragmentSet> result = composer.Compose(request, fragmentSet);
                diagnostics.AddRange(result.Diagnostics);
                PlanFragmentSet? composed = result.Value;
                if (!result.Succeeded || composed is null)
                {
                    if (!HasErrors(result.Diagnostics))
                        diagnostics.Add(Error("missing-composition-result", $"Composer '{composer.Key}' returned no fragments."));
                    return new PackageResult<InstallPlan>(null, diagnostics);
                }
                fragmentSet = composed;
            }
            return InstallPlanComposition.Compose(request, changes, fragmentSet, diagnostics);
        }

        internal static bool HasErrors(IEnumerable<PackageDiagnostic> diagnostics) =>
            diagnostics.Any(x => x.Severity == PackageDiagnosticSeverity.Error);

        internal static PackageDiagnostic Error(string code, string message, PackageId? id = null) =>
            PackageDiagnostic.Error("package:" + code, PackageDiagnosticPhase.Planning, message, id.HasValue ? new[] { id.Value } : null);
    }
}
