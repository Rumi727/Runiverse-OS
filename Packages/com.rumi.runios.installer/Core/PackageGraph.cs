#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Exposes one validated selection without installation instructions.<br/>
    /// 설치 지시 없이 검증된 선택 하나를 제공합니다.
    /// </summary>
    public sealed class ResolvedPackage
    {
        /// <summary>
        /// Gets the exact package identity; installed observations may report an unknown identity.<br/>
        /// 정확한 패키지 식별자를 가져오며 설치 관측은 알 수 없는 식별자를 보고할 수 있습니다.
        /// </summary>
        public PackageIdentity Identity { get; }
        /// <summary>
        /// Gets dependency metadata retrieved for the selected exact revision.<br/>
        /// 선택한 정확한 리비전에서 조회한 의존성 메타데이터를 가져옵니다.
        /// </summary>
        public PackageMetadata Metadata { get; }
        /// <summary>
        /// Gets immutable content references without acquiring their content.<br/>
        /// 콘텐츠를 획득하지 않고 불변 콘텐츠 참조를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageArtifactReference> Artifacts { get; }

        internal ResolvedPackage(PackageCandidate candidate, PackageMetadata metadata)
        {
            Identity = candidate.Identity;
            Artifacts = candidate.Artifacts;
            Metadata = metadata;
        }
    }

    /// <summary>
    /// Retains the declaration that connects two resolved packages.<br/>
    /// 해석된 두 패키지를 연결하는 선언을 보존합니다.
    /// </summary>
    public sealed class ResolvedDependency
    {
        /// <summary>
        /// Gets the package that declares this dependency edge.<br/>
        /// 이 의존성 간선을 선언한 패키지를 가져옵니다.
        /// </summary>
        public PackageId Owner { get; }
        /// <summary>
        /// Gets the selected dependency package identifier.<br/>
        /// 선택된 의존성 패키지 식별자를 가져옵니다.
        /// </summary>
        public PackageId Dependency => Requirement.Id;
        /// <summary>
        /// Gets the original dependency requirement.<br/>
        /// 원본 의존성 요구를 가져옵니다.
        /// </summary>
        public PackageRequirement Requirement { get; }

        internal ResolvedDependency(PackageId owner, PackageRequirement requirement)
        {
            Owner = owner;
            Requirement = requirement;
        }
    }

    /// <summary>
    /// Exposes a complete, acyclic selection graph constructed only by the resolver.<br/>
    /// 리졸버만 생성하는 완전하고 순환 없는 선택 그래프를 제공합니다.
    /// </summary>
    public sealed class ResolvedPackageGraph
    {
        /// <summary>
        /// Gets the immutable resolved nodes indexed by logical package identifier.<br/>
        /// 논리적 패키지 식별자로 인덱싱한 불변 해석 노드를 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, ResolvedPackage> Packages { get; }
        /// <summary>
        /// Gets all retained root requirements used for this resolution.<br/>
        /// 이 해석에 사용하는 유지할 전체 루트 요구를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> Roots { get; }
        /// <summary>
        /// Gets immutable dependency declarations, edges, or effective observed identifiers.<br/>
        /// 불변 의존성 선언, 간선 또는 실제 관측된 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<ResolvedDependency> Dependencies { get; }
        /// <summary>
        /// Gets a deterministic order in which dependencies precede their dependents.<br/>
        /// 의존 패키지가 이를 사용하는 패키지보다 앞서는 결정적인 순서를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageId> DependencyOrder { get; }

        internal ResolvedPackageGraph(IEnumerable<ResolvedPackage> packages, IEnumerable<PackageRequirement> roots)
        {
            Packages = Snapshots.Map(packages.Select(x => new KeyValuePair<PackageId, ResolvedPackage>(x.Identity.Id, x)));
            Roots = Snapshots.List(roots);
            Dependencies = Snapshots.List(Packages.Values.OrderBy(x => x.Identity.Id.Value, StringComparer.Ordinal)
                .SelectMany(x => x.Metadata.Dependencies.Select(y => new ResolvedDependency(x.Identity.Id, y.Requirement))));
            GraphOrder.TryOrder(Packages.Keys.OrderBy(x => x.Value, StringComparer.Ordinal),
                id => Packages[id].Metadata.Dependencies.Select(x => x.Requirement.Id), out IReadOnlyList<PackageId> order);
            DependencyOrder = order;
        }
    }

    /// <summary>
    /// Stores the complete root intent and explicit source overrides for one resolution.<br/>
    /// 한 번의 해석에 사용할 전체 루트 의도와 명시적 출처 재지정을 저장합니다.
    /// </summary>
    public sealed class ResolutionRequest
    {
        /// <summary>
        /// Gets all retained root requirements used for this resolution.<br/>
        /// 이 해석에 사용하는 유지할 전체 루트 요구를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> Roots { get; }
        /// <summary>
        /// Gets explicit source bindings that override declaration hints.<br/>
        /// 선언 힌트보다 우선하는 명시적 출처 바인딩을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, PackageSource> SourceBindings { get; }
        /// <summary>
        /// Gets the upper bound on attempted candidate branches.<br/>
        /// 시도할 후보 분기의 상한을 가져옵니다.
        /// </summary>
        public int MaximumCandidateAttempts { get; }

        /// <summary>
        /// Snapshots retained root intent and source overrides with a bounded search budget.<br/>
        /// 제한된 탐색 예산과 함께 유지할 루트 의도와 출처 재지정을 저장합니다.
        /// </summary>
        /// <param name="roots">
        /// All roots that should remain in the resolved closure.<br/>
        /// 해석된 전체 의존성 집합에 유지해야 하는 모든 루트입니다.
        /// </param>
        /// <param name="sourceBindings">
        /// Explicit source overrides indexed by logical package identifier.<br/>
        /// 논리적 패키지 식별자로 색인한 명시적 출처 재지정입니다.
        /// </param>
        /// <param name="maximumCandidateAttempts">
        /// A positive upper bound on attempted candidate branches.<br/>
        /// 시도할 후보 분기에 대한 양수 상한입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a root or binding is invalid or source binding keys are duplicated.<br/>
        /// 루트 또는 바인딩이 유효하지 않거나 출처 바인딩 키가 중복되면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a numeric or enumeration argument is outside its accepted range.<br/>
        /// 숫자 또는 열거형 인수가 허용 범위를 벗어나면 발생합니다.
        /// </exception>
        public ResolutionRequest(IEnumerable<PackageRequirement> roots,
            IEnumerable<KeyValuePair<PackageId, PackageSource>>? sourceBindings = null, int maximumCandidateAttempts = 1024)
        {
            Roots = Snapshots.List(roots);
            SourceBindings = Snapshots.Map(sourceBindings ?? Array.Empty<KeyValuePair<PackageId, PackageSource>>());
            foreach (PackageId id in SourceBindings.Keys)
                Snapshots.Id(id, nameof(sourceBindings));
            if (maximumCandidateAttempts < 1)
                throw new ArgumentOutOfRangeException(nameof(maximumCandidateAttempts));
            MaximumCandidateAttempts = maximumCandidateAttempts;
        }
    }

    /// <summary>
    /// Identifies one resolution's lifetime for provider-owned snapshot caches.<br/>
    /// 프로바이더 소유 스냅샷 캐시를 위해 한 해석의 수명을 식별합니다.
    /// </summary>
    /// <remarks>
    /// Providers may use this object as a weak cache key. It does not contain services or mutable extension state.<br/>
    /// 프로바이더는 이 객체를 약한 캐시 키로 사용할 수 있으며 서비스나 가변 확장 상태를 포함하지 않습니다.
    /// </remarks>
    public sealed class ResolutionSession
    {
        /// <summary>
        /// Gets the unique identity of this resolution invocation.<br/>
        /// 이 해석 호출의 고유 식별자를 가져옵니다.
        /// </summary>
        public Guid Id { get; } = Guid.NewGuid();

        internal ResolutionSession() { }
    }

    /// <summary>
    /// Supplies a bound source and all currently known requirements to a catalog.<br/>
    /// 바인딩된 출처와 현재 알려진 모든 요구를 카탈로그에 제공합니다.
    /// </summary>
    public sealed class PackageQuery
    {
        /// <summary>
        /// Gets the shared resolution lifetime for consistent provider snapshots.<br/>
        /// 일관된 프로바이더 스냅샷을 위한 공유 해석 수명을 가져옵니다.
        /// </summary>
        public ResolutionSession Session { get; }
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId Id { get; }
        /// <summary>
        /// Gets the canonical source bound to this catalog query.<br/>
        /// 이 카탈로그 쿼리에 바인딩된 정규화된 출처를 가져옵니다.
        /// </summary>
        public PackageSource Source { get; }
        /// <summary>
        /// Gets all currently known requirements for the queried package.<br/>
        /// 조회할 패키지에 대해 현재 알려진 모든 요구를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageRequirement> Requirements { get; }

        internal PackageQuery(PackageId id, PackageSource source, IEnumerable<PackageRequirement> requirements, ResolutionSession session)
        {
            Session = session;
            Id = id;
            Source = source;
            Requirements = Snapshots.List(requirements);
        }
    }

    /// <summary>
    /// Captures candidates in provider preference order and reports search completeness.<br/>
    /// 프로바이더 선호 순서의 후보와 검색 완전성을 저장합니다.
    /// </summary>
    public sealed class CandidateSet
    {
        /// <summary>
        /// Gets exact candidates in deterministic provider preference order.<br/>
        /// 결정적인 프로바이더 선호 순서의 정확한 후보를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageCandidate> Candidates { get; }
        /// <summary>
        /// Gets whether discovery covered the complete requested candidate or installed state.<br/>
        /// 검색이 요청한 후보 또는 설치 상태 전체를 포함하는지 여부를 가져옵니다.
        /// </summary>
        public bool IsComplete { get; }

        /// <summary>
        /// Snapshots exact candidates in provider preference order.<br/>
        /// 프로바이더 선호 순서로 정확한 후보를 저장합니다.
        /// </summary>
        /// <param name="candidates">
        /// Exact candidates in deterministic preference order.<br/>
        /// 결정적인 선호 순서의 정확한 후보입니다.
        /// </param>
        /// <param name="isComplete">
        /// Whether discovery covers the complete requested state.<br/>
        /// 검색이 요청한 전체 상태를 포함하는지 여부입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="candidates"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="candidates"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public CandidateSet(IEnumerable<PackageCandidate> candidates, bool isComplete = true)
        {
            Candidates = Snapshots.List(candidates);
            IsComplete = isComplete;
        }
    }

    /// <summary>
    /// Reports whether an evaluator accepts a candidate, together with diagnostics.<br/>
    /// 평가기의 후보 허용 여부와 진단을 제공합니다.
    /// </summary>
    public sealed class ConstraintMatch
    {
        /// <summary>
        /// Gets whether the evaluated constraint accepts the candidate.<br/>
        /// 평가한 제약이 후보를 허용하는지 여부를 가져옵니다.
        /// </summary>
        public bool IsMatch { get; }
        /// <summary>
        /// Gets the immutable diagnostics accompanying this result.<br/>
        /// 이 결과와 함께 제공되는 불변 진단을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageDiagnostic> Diagnostics { get; }

        /// <summary>
        /// Snapshots a candidate acceptance decision and its diagnostics.<br/>
        /// 후보 허용 결정과 진단을 저장합니다.
        /// </summary>
        /// <param name="isMatch">
        /// Whether the evaluated candidate satisfies the constraint.<br/>
        /// 평가한 후보가 제약을 만족하는지 여부입니다.
        /// </param>
        /// <param name="diagnostics">
        /// Diagnostics accompanying the result or decision.<br/>
        /// 결과 또는 결정과 함께 제공되는 진단입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="diagnostics"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="diagnostics"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public ConstraintMatch(bool isMatch, IEnumerable<PackageDiagnostic>? diagnostics = null)
        {
            IsMatch = isMatch;
            Diagnostics = Snapshots.List(diagnostics ?? Array.Empty<PackageDiagnostic>());
        }
    }
}
