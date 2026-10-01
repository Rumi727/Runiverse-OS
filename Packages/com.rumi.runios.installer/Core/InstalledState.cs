#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Captures an effective installed package and its management ownership.<br/>
    /// 실제 사용 중인 설치 패키지와 관리 소유권을 저장합니다.
    /// </summary>
    public sealed class InstalledPackage
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId Id { get; }
        /// <summary>
        /// Gets the exact package identity; installed observations may report an unknown identity.<br/>
        /// 정확한 패키지 식별자를 가져오며 설치 관측은 알 수 없는 식별자를 보고할 수 있습니다.
        /// </summary>
        public PackageIdentity? Identity { get; }
        /// <summary>
        /// Gets the passive representation used by the installed package.<br/>
        /// 설치 패키지가 사용하는 수동적 표현을 가져옵니다.
        /// </summary>
        public PackageInstallation Installation { get; }
        /// <summary>
        /// Gets the owner key; an absent owner does not grant management ownership.<br/>
        /// 소유자 키를 가져오며 소유자가 없으면 관리 소유권을 부여하지 않습니다.
        /// </summary>
        public string? ManagementOwner { get; }
        /// <summary>
        /// Gets immutable dependency declarations, edges, or effective observed identifiers.<br/>
        /// 불변 의존성 선언, 간선 또는 실제 관측된 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageId> Dependencies { get; }
        /// <summary>
        /// Gets observed dependency declarations used to verify changes required by retained packages.<br/>
        /// 유지할 패키지가 요구하는 변경을 검증할 때 사용할 관측된 의존성 선언을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> DependencyRequirements { get; }

        /// <summary>
        /// Snapshots effective identity, installation representation, and observed dependencies.<br/>
        /// 실제 식별자, 설치 표현 및 관측된 의존성을 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="identity">
        /// The exact identity, or an unknown identity for installed observations.<br/>
        /// 정확한 식별자 또는 설치 관측의 알 수 없는 식별자입니다.
        /// </param>
        /// <param name="installation">
        /// The effective passive installation representation.<br/>
        /// 실제 수동적 설치 표현입니다.
        /// </param>
        /// <param name="managementOwner">
        /// The management owner key; omission denotes external ownership where allowed.<br/>
        /// 관리 소유자 키이며 허용되는 위치에서 생략하면 외부 소유를 나타냅니다.
        /// </param>
        /// <param name="dependencies">
        /// Dependency observations or declarations; omitted values produce an empty snapshot.<br/>
        /// 의존성 관측 또는 선언이며 생략하면 빈 스냅샷을 생성합니다.
        /// </param>
        /// <param name="dependencyRequirements">
        /// Observed dependency declarations; omission leaves requirement compatibility unknown.<br/>
        /// 관측된 의존성 선언이며 생략하면 요구 호환성을 알 수 없는 상태로 둡니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when identifiers disagree or are uninitialized, owner text is empty, or a declaration is <see langword="null"/>.<br/>
        /// 식별자가 일치하지 않거나 초기화되지 않았거나 소유자 문자열이 비어 있거나 선언이 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public InstalledPackage(PackageId id, PackageIdentity? identity, PackageInstallation installation,
            string? managementOwner = null, IEnumerable<PackageId>? dependencies = null,
            IEnumerable<PackageRequirement>? dependencyRequirements = null)
        {
            Id = Snapshots.Id(id, nameof(id));
            if (identity is not null && identity.Id != id)
                throw new ArgumentException("The installed identity must match the package identifier.", nameof(identity));
            Identity = identity;
            Installation = installation ?? throw new ArgumentNullException(nameof(installation));
            ManagementOwner = managementOwner is null ? null : Snapshots.Text(managementOwner, nameof(managementOwner));
            DependencyRequirements = Snapshots.List(dependencyRequirements ?? Array.Empty<PackageRequirement>());
            Dependencies = Snapshots.List((dependencies ?? Array.Empty<PackageId>())
                .Concat(DependencyRequirements.Select(x => x.Id)).Distinct());
            foreach (PackageId dependency in Dependencies)
                Snapshots.Id(dependency, nameof(dependencies));
        }
    }

    /// <summary>
    /// Exposes immutable effective observations, including unknown revisions and incomplete discovery.<br/>
    /// 알 수 없는 리비전과 불완전한 조회를 포함하는 불변 관측 결과를 제공합니다.
    /// </summary>
    public sealed class InstalledPackageGraph
    {
        /// <summary>
        /// Gets effective installed observations indexed by logical package identifier.<br/>
        /// 논리적 패키지 식별자로 인덱싱한 실제 설치 관측을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, InstalledPackage> Packages { get; }
        /// <summary>
        /// Gets whether discovery covered the complete requested candidate or installed state.<br/>
        /// 검색이 요청한 후보 또는 설치 상태 전체를 포함하는지 여부를 가져옵니다.
        /// </summary>
        public bool IsComplete { get; }

        InstalledPackageGraph(Dictionary<PackageId, InstalledPackage> packages, bool isComplete)
        {
            Packages = Snapshots.Map(packages);
            IsComplete = isComplete;
        }

        /// <summary>
        /// Freezes normalized observations and reports duplicate effective package identifiers.<br/>
        /// 정규화된 관측을 고정하고 실제 패키지 식별자의 중복을 진단합니다.
        /// </summary>
        /// <param name="packages">
        /// Effective packages after environment precedence has been applied.<br/>
        /// 환경 우선순위를 적용한 실제 패키지입니다.
        /// </param>
        /// <param name="isComplete">
        /// Whether package discovery and dependency observations are complete.<br/>
        /// 패키지 조회와 의존성 관측이 완전한지 여부입니다.
        /// </param>
        /// <returns>
        /// The graph or diagnostics when effective identifiers are duplicated.<br/>
        /// 그래프를 반환하거나 실제 식별자가 중복되면 진단을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="packages"/> is <see langword="null"/>.<br/>
        /// <paramref name="packages"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="packages"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="packages"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public static PackageResult<InstalledPackageGraph> Create(IEnumerable<InstalledPackage> packages, bool isComplete = true)
        {
            IReadOnlyList<InstalledPackage> snapshot = Snapshots.List(packages);
            Dictionary<PackageId, InstalledPackage> indexed = new();
            List<PackageDiagnostic> diagnostics = new();
            foreach (InstalledPackage package in snapshot)
            {
                if (!indexed.TryAdd(package.Id, package))
                    diagnostics.Add(PackageDiagnostic.Error("package:duplicate-installed-id", PackageDiagnosticPhase.Observation,
                        $"Effective observations contain duplicate package '{package.Id}'.", new[] { package.Id }));
            }
            if (diagnostics.Count > 0)
                return new PackageResult<InstalledPackageGraph>(null, diagnostics);
            return new PackageResult<InstalledPackageGraph>(new InstalledPackageGraph(indexed, isComplete));
        }
    }

    /// <summary>
    /// Supplies immutable environment facts that a matching planning provider can interpret.<br/>
    /// 해당 계획 프로바이더가 해석할 수 있는 불변 환경 정보를 제공합니다.
    /// </summary>
    public abstract class EnvironmentFact { }

    /// <summary>
    /// Captures effective packages, resource fingerprints, and passive environment facts.<br/>
    /// 실제 패키지, 리소스 지문 및 수동적 환경 정보를 저장합니다.
    /// </summary>
    public sealed class InstalledEnvironmentSnapshot
    {
        /// <summary>
        /// Gets the passive environment descriptor associated with these observations or operations.<br/>
        /// 이 관측 또는 작업과 연결된 수동적 환경 설명자를 가져옵니다.
        /// </summary>
        public InstallationTarget Target { get; }
        /// <summary>
        /// Gets the immutable effective package graph or its package index.<br/>
        /// 불변 실제 패키지 그래프 또는 패키지 색인을 가져옵니다.
        /// </summary>
        public InstalledPackageGraph Packages { get; }
        /// <summary>
        /// Gets observed opaque resource fingerprints for later execution precondition checks.<br/>
        /// 이후 실행 전제조건 검사에 사용할 관측된 불투명 리소스 지문을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, string> ResourceFingerprints { get; }
        /// <summary>
        /// Gets immutable environment facts interpreted only by matching adapters.<br/>
        /// 해당 어댑터만 해석하는 불변 환경 정보를 가져옵니다.
        /// </summary>
        public IReadOnlyList<EnvironmentFact> Facts { get; }

        /// <summary>
        /// Snapshots observed packages, resources, and environment facts.<br/>
        /// 관측한 패키지, 리소스 및 환경 정보를 저장합니다.
        /// </summary>
        /// <param name="target">
        /// The passive environment descriptor.<br/>
        /// 수동적 환경 설명자입니다.
        /// </param>
        /// <param name="packages">
        /// Normalized effective package observations.<br/>
        /// 정규화된 실제 패키지 관측입니다.
        /// </param>
        /// <param name="resourceFingerprints">
        /// Observed opaque resource states.<br/>
        /// 관측된 불투명 리소스 상태입니다.
        /// </param>
        /// <param name="facts">
        /// Immutable adapter-specific environment facts.<br/>
        /// 불변 어댑터별 환경 정보입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when resource entries are invalid or duplicated, or a fact is <see langword="null"/>.<br/>
        /// 리소스 항목이 유효하지 않거나 중복되거나 환경 정보가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public InstalledEnvironmentSnapshot(InstallationTarget target, InstalledPackageGraph packages,
            IEnumerable<KeyValuePair<string, string>>? resourceFingerprints = null, IEnumerable<EnvironmentFact>? facts = null)
        {
            Target = target ?? throw new ArgumentNullException(nameof(target));
            Packages = packages ?? throw new ArgumentNullException(nameof(packages));
            ResourceFingerprints = Snapshots.Map(resourceFingerprints ?? Array.Empty<KeyValuePair<string, string>>());
            foreach (string resource in ResourceFingerprints.Keys)
                Snapshots.Text(resource, nameof(resourceFingerprints));
            Facts = Snapshots.List(facts ?? Array.Empty<EnvironmentFact>());
        }
    }
}
