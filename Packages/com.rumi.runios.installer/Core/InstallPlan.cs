#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Classifies a package-state transition independently of concrete operations.<br/>
    /// 구체적인 작업과 독립적으로 패키지 상태 전이를 분류합니다.
    /// </summary>
    public enum PackageChangeKind { Install, Update, Remove, ChangeInstallation }

    /// <summary>
    /// Identifies a package's installation or retirement boundary.<br/>
    /// 패키지의 설치 또는 제거 경계를 식별합니다.
    /// </summary>
    public enum PackageOperationPhase { Install, Remove }

    /// <summary>
    /// Describes access to an opaque resource key.<br/>
    /// 불투명한 리소스 키에 대한 접근을 나타냅니다.
    /// </summary>
    public enum ResourceAccess { Read, Write }

    /// <summary>
    /// Describes one required change for preview and installation providers.<br/>
    /// 미리 보기와 설치 프로바이더에 필요한 변경 하나를 나타냅니다.
    /// </summary>
    public sealed class PackageChange
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId Id { get; }
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public PackageChangeKind Kind { get; }
        /// <summary>
        /// Gets the observed environment or package being compared.<br/>
        /// 비교할 관측 환경 또는 패키지를 가져옵니다.
        /// </summary>
        public InstalledPackage? Current { get; }
        /// <summary>
        /// Gets the resolved graph or package that the target must represent.<br/>
        /// 대상이 표현해야 하는 해석된 그래프 또는 패키지를 가져옵니다.
        /// </summary>
        public ResolvedPackage? Desired { get; }
        /// <summary>
        /// Gets the requested installation representation, absent for removals.<br/>
        /// 요청된 설치 표현을 가져오며 제거 시에는 존재하지 않습니다.
        /// </summary>
        public PackageInstallation? DesiredInstallation { get; }

        internal PackageChange(PackageId id, PackageChangeKind kind, InstalledPackage? current,
            ResolvedPackage? desired, PackageInstallation? desiredInstallation)
        {
            Id = id;
            Kind = kind;
            Current = current;
            Desired = desired;
            DesiredInstallation = desiredInstallation;
        }
    }

    /// <summary>
    /// Supplies desired state, observed state, installation bindings, and explicit ownership policy.<br/>
    /// 원하는 상태, 관측 상태, 설치 바인딩 및 명시적 소유권 정책을 제공합니다.
    /// </summary>
    public sealed class PlanningRequest
    {
        /// <summary>
        /// Gets the resolved graph or package that the target must represent.<br/>
        /// 대상이 표현해야 하는 해석된 그래프 또는 패키지를 가져옵니다.
        /// </summary>
        public ResolvedPackageGraph Desired { get; }
        /// <summary>
        /// Gets the observed environment or package being compared.<br/>
        /// 비교할 관측 환경 또는 패키지를 가져옵니다.
        /// </summary>
        public InstalledEnvironmentSnapshot Current { get; }
        /// <summary>
        /// Gets explicit installation bindings for every desired package.<br/>
        /// 원하는 모든 패키지의 명시적 설치 바인딩을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, PackageInstallation> Installations { get; }
        /// <summary>
        /// Gets the owner key; an absent owner does not grant management ownership.<br/>
        /// 소유자 키를 가져오며 소유자가 없으면 관리 소유권을 부여하지 않습니다.
        /// </summary>
        public string ManagementOwner { get; }
        /// <summary>
        /// Gets whether unused packages belonging to the selected management owner may be removed.<br/>
        /// 선택한 관리 소유자의 사용하지 않는 패키지를 제거할 수 있는지 여부를 가져옵니다.
        /// </summary>
        public bool RemoveUnusedOwnedPackages { get; }
        /// <summary>
        /// Gets whether changing externally owned packages is explicitly permitted.<br/>
        /// 외부 소유 패키지 변경을 명시적으로 허용하는지 여부를 가져옵니다.
        /// </summary>
        public bool AllowAdoption { get; }

        /// <summary>
        /// Snapshots desired installation bindings and explicit ownership permissions.<br/>
        /// 원하는 설치 바인딩과 명시적 소유권 권한을 저장합니다.
        /// </summary>
        /// <param name="desired">
        /// The complete validated desired graph.<br/>
        /// 완전히 검증된 원하는 그래프입니다.
        /// </param>
        /// <param name="current">
        /// The effective installed environment snapshot.<br/>
        /// 실제 설치 환경 스냅샷입니다.
        /// </param>
        /// <param name="installations">
        /// Explicit bindings for every desired package.<br/>
        /// 원하는 모든 패키지의 명시적 바인딩입니다.
        /// </param>
        /// <param name="managementOwner">
        /// The management owner key; omission denotes external ownership where allowed.<br/>
        /// 관리 소유자 키이며 허용되는 위치에서 생략하면 외부 소유를 나타냅니다.
        /// </param>
        /// <param name="removeUnusedOwnedPackages">
        /// Whether unused packages belonging to this owner may be removed.<br/>
        /// 이 소유자의 사용하지 않는 패키지를 제거할 수 있는지 여부입니다.
        /// </param>
        /// <param name="allowAdoption">
        /// Whether externally owned packages may be changed.<br/>
        /// 외부 소유 패키지를 변경할 수 있는지 여부입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when bindings are invalid or duplicated, or owner text is empty or whitespace.<br/>
        /// 바인딩이 유효하지 않거나 중복되거나 소유자 문자열이 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public PlanningRequest(ResolvedPackageGraph desired, InstalledEnvironmentSnapshot current,
            IEnumerable<KeyValuePair<PackageId, PackageInstallation>> installations, string managementOwner,
            bool removeUnusedOwnedPackages = false, bool allowAdoption = false)
        {
            Desired = desired ?? throw new ArgumentNullException(nameof(desired));
            Current = current ?? throw new ArgumentNullException(nameof(current));
            Installations = Snapshots.Map(installations);
            foreach (PackageId id in Installations.Keys)
                Snapshots.Id(id, nameof(installations));
            ManagementOwner = Snapshots.Text(managementOwner, nameof(managementOwner));
            RemoveUnusedOwnedPackages = removeUnusedOwnedPackages;
            AllowAdoption = allowAdoption;
        }
    }

    /// <summary>
    /// Supplies global context and the installation or retirement transitions assigned to one provider.<br/>
    /// 한 프로바이더에 할당한 설치 또는 제거 전이와 전체 문맥을 제공합니다.
    /// </summary>
    public sealed class InstallationPlanningContext
    {
        /// <summary>
        /// Gets the full immutable planning context shared across providers.<br/>
        /// 프로바이더 간 공유하는 전체 불변 계획 문맥을 가져옵니다.
        /// </summary>
        public PlanningRequest Request { get; }
        /// <summary>
        /// Gets the routing key for the installation mechanism.<br/>
        /// 설치 방법의 라우팅 키를 가져옵니다.
        /// </summary>
        public string InstallationKind { get; }
        /// <summary>
        /// Gets the install or update transitions assigned to this provider.<br/>
        /// 이 프로바이더에 할당한 설치 또는 갱신 전이를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageChange> InstallChanges { get; }
        /// <summary>
        /// Gets the retirement transitions assigned to this provider.<br/>
        /// 이 프로바이더에 할당한 제거 전이를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageChange> RemoveChanges { get; }

        internal InstallationPlanningContext(PlanningRequest request, string kind, IEnumerable<PackageChange> changes)
        {
            Request = request;
            InstallationKind = kind;
            InstallChanges = Snapshots.List(changes.Where(x => x.DesiredInstallation?.Kind == kind));
            RemoveChanges = Snapshots.List(changes.Where(x => x.Current is { } current && current.Installation.Kind == kind &&
                (x.DesiredInstallation is null || !Snapshots.SameInstallation(current.Installation, x.DesiredInstallation))));
        }
    }

    /// <summary>
    /// Stores an immutable operation payload without execution behavior.<br/>
    /// 실행 동작 없이 불변 작업 데이터를 저장합니다.
    /// </summary>
    public abstract class InstallOperation
    {
        /// <summary>
        /// Gets the routing key or transition category consumed by the corresponding provider.<br/>
        /// 해당 프로바이더가 사용하는 라우팅 키 또는 전이 분류를 가져옵니다.
        /// </summary>
        public string Kind { get; }

        /// <summary>
        /// Assigns passive operation data to an executor capability.<br/>
        /// 수동적 작업 데이터를 실행기 기능에 할당합니다.
        /// </summary>
        /// <param name="kind">
        /// The namespaced capability routing key.<br/>
        /// 네임스페이스를 가진 기능 라우팅 키입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="kind"/> is empty or whitespace.<br/>
        /// <paramref name="kind"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        protected InstallOperation(string kind) => Kind = Snapshots.Text(kind, nameof(kind));
    }

    /// <summary>
    /// Declares abstract resource access for generic conflict checks.<br/>
    /// 공통 충돌 검사에 사용할 추상 리소스 접근을 선언합니다.
    /// </summary>
    public sealed class ResourceClaim
    {
        /// <summary>
        /// Gets the canonical opaque resource identifier used for equality-based conflict checks.<br/>
        /// 동등성 기반 충돌 검사에 사용할 정규화된 불투명 리소스 식별자를 가져옵니다.
        /// </summary>
        public string Resource { get; }
        /// <summary>
        /// Gets the declared read or write access to the resource.<br/>
        /// 선언된 리소스 읽기 또는 쓰기 접근을 가져옵니다.
        /// </summary>
        public ResourceAccess Access { get; }

        /// <summary>
        /// Captures resource access without interpreting resource semantics.<br/>
        /// 리소스 의미를 해석하지 않고 리소스 접근을 저장합니다.
        /// </summary>
        /// <param name="resource">
        /// The canonical opaque resource key.<br/>
        /// 정규화된 불투명 리소스 키입니다.
        /// </param>
        /// <param name="access">
        /// The declared read or write access.<br/>
        /// 선언된 읽기 또는 쓰기 접근입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="resource"/> is empty or whitespace.<br/>
        /// <paramref name="resource"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a numeric or enumeration argument is outside its accepted range.<br/>
        /// 숫자 또는 열거형 인수가 허용 범위를 벗어나면 발생합니다.
        /// </exception>
        public ResourceClaim(string resource, ResourceAccess access)
        {
            Resource = Snapshots.Text(resource, nameof(resource));
            if (access != ResourceAccess.Read && access != ResourceAccess.Write)
                throw new ArgumentOutOfRangeException(nameof(access));
            Access = access;
        }
    }

    /// <summary>
    /// Couples an operation payload to immutable prerequisite and resource declarations.<br/>
    /// 작업 데이터를 불변 선행 작업 및 리소스 선언과 연결합니다.
    /// </summary>
    public sealed class PlannedOperation
    {
        /// <summary>
        /// Gets the logical identifier of this package or operation.<br/>
        /// 이 패키지 또는 작업의 논리적 식별자를 가져옵니다.
        /// </summary>
        public string Id { get; }
        /// <summary>
        /// Gets the passive payload executed by a matching executor.<br/>
        /// 해당 실행기가 실행할 수동적 데이터를 가져옵니다.
        /// </summary>
        public InstallOperation Operation { get; }
        /// <summary>
        /// Gets operation identifiers that must complete before this operation.<br/>
        /// 이 작업 전에 완료해야 하는 작업 식별자를 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> Prerequisites { get; }
        /// <summary>
        /// Gets resource access declarations used for conflict detection.<br/>
        /// 충돌 탐지에 사용할 리소스 접근 선언을 가져옵니다.
        /// </summary>
        public IReadOnlyList<ResourceClaim> Claims { get; }

        /// <summary>
        /// Snapshots operation prerequisites and declared resource access.<br/>
        /// 작업의 선행 조건과 선언된 리소스 접근을 저장합니다.
        /// </summary>
        /// <param name="id">
        /// The initialized logical package or operation identifier.<br/>
        /// 초기화된 논리적 패키지 또는 작업 식별자입니다.
        /// </param>
        /// <param name="operation">
        /// The immutable operation payload.<br/>
        /// 불변 작업 데이터입니다.
        /// </param>
        /// <param name="prerequisites">
        /// Operation identifiers that must complete first.<br/>
        /// 먼저 완료해야 하는 작업 식별자입니다.
        /// </param>
        /// <param name="claims">
        /// Declared abstract resource access.<br/>
        /// 선언된 추상 리소스 접근입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when operation identifiers are empty or whitespace, or resource claims contain a <see langword="null"/> element.<br/>
        /// 작업 식별자가 비어 있거나 공백이거나 리소스 접근 선언에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PlannedOperation(string id, InstallOperation operation, IEnumerable<string>? prerequisites = null,
            IEnumerable<ResourceClaim>? claims = null)
        {
            Id = Snapshots.Text(id, nameof(id));
            Operation = operation ?? throw new ArgumentNullException(nameof(operation));
            Prerequisites = Snapshots.List(prerequisites ?? Array.Empty<string>());
            foreach (string prerequisite in Prerequisites)
                Snapshots.Text(prerequisite, nameof(prerequisites));
            Claims = Snapshots.List(claims ?? Array.Empty<ResourceClaim>());
        }
    }

    /// <summary>
    /// Exports operation boundaries for cross-provider dependency and replacement ordering.<br/>
    /// 프로바이더 간 의존성 및 교체 순서에 사용할 작업 경계를 제공합니다.
    /// </summary>
    /// <remarks>
    /// Atomic operations may be shared by multiple package boundaries. Boundaries must expose all target-changing operations.<br/>
    /// 여러 패키지 경계가 하나의 원자적 작업을 공유할 수 있으며 경계는 대상 변경 작업을 모두 포함해야 합니다.
    /// </remarks>
    public sealed class PackageOperationBoundary
    {
        /// <summary>
        /// Gets the package whose lifecycle boundary is exported.<br/>
        /// 생명주기 경계를 제공할 패키지를 가져옵니다.
        /// </summary>
        public PackageId PackageId { get; }
        /// <summary>
        /// Gets the diagnostic stage or package lifecycle phase.<br/>
        /// 진단 단계 또는 패키지 생명주기 단계를 가져옵니다.
        /// </summary>
        public PackageOperationPhase Phase { get; }
        /// <summary>
        /// Gets operations that begin target changes for this lifecycle phase.<br/>
        /// 이 생명주기 단계의 대상 변경을 시작하는 작업을 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> StartOperations { get; }
        /// <summary>
        /// Gets operations whose completion establishes the lifecycle phase.<br/>
        /// 완료 시 생명주기 단계가 성립되는 작업을 가져옵니다.
        /// </summary>
        public IReadOnlyList<string> CompletionOperations { get; }

        /// <summary>
        /// Snapshots target-change entry and completion operations for one package phase.<br/>
        /// 패키지 단계 하나의 대상 변경 시작 및 완료 작업을 저장합니다.
        /// </summary>
        /// <param name="packageId">
        /// The package whose lifecycle phase is represented.<br/>
        /// 생명주기 단계를 표현할 패키지입니다.
        /// </param>
        /// <param name="phase">
        /// The diagnostic stage or package lifecycle phase.<br/>
        /// 진단 단계 또는 패키지 생명주기 단계입니다.
        /// </param>
        /// <param name="startOperations">
        /// Target-changing entry operations for the phase.<br/>
        /// 단계의 대상 변경 시작 작업입니다.
        /// </param>
        /// <param name="completionOperations">
        /// Operations establishing phase completion.<br/>
        /// 단계 완료를 성립시키는 작업입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the package identifier is uninitialized or operation identifiers are empty or invalid.<br/>
        /// 패키지 식별자가 초기화되지 않았거나 작업 식별자가 비어 있거나 유효하지 않으면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a numeric or enumeration argument is outside its accepted range.<br/>
        /// 숫자 또는 열거형 인수가 허용 범위를 벗어나면 발생합니다.
        /// </exception>
        public PackageOperationBoundary(PackageId packageId, PackageOperationPhase phase,
            IEnumerable<string> startOperations, IEnumerable<string> completionOperations)
        {
            PackageId = Snapshots.Id(packageId, nameof(packageId));
            if (phase != PackageOperationPhase.Install && phase != PackageOperationPhase.Remove)
                throw new ArgumentOutOfRangeException(nameof(phase));
            Phase = phase;
            StartOperations = Snapshots.List(startOperations);
            CompletionOperations = Snapshots.List(completionOperations);
            foreach (string id in StartOperations.Concat(CompletionOperations))
                Snapshots.Text(id, nameof(startOperations));
        }
    }

    /// <summary>
    /// Returns passive operations and the package lifecycle boundaries they implement.<br/>
    /// 구현할 패키지 생명주기 경계와 수동적 작업을 반환합니다.
    /// </summary>
    public sealed class PlanFragment
    {
        /// <summary>
        /// Gets passive operations; validated plans expose them in prerequisite order.<br/>
        /// 수동적 작업을 가져오며 검증된 계획은 선행 순서로 제공합니다.
        /// </summary>
        public IReadOnlyList<PlannedOperation> Operations { get; }
        /// <summary>
        /// Gets exported package lifecycle boundaries for cross-provider ordering.<br/>
        /// 프로바이더 간 순서에 사용할 패키지 생명주기 경계를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageOperationBoundary> Boundaries { get; }

        /// <summary>
        /// Snapshots contributed operations and exported package boundaries.<br/>
        /// 제공된 작업과 패키지 경계를 저장합니다.
        /// </summary>
        /// <param name="operations">
        /// Passive contributed operations.<br/>
        /// 제공된 수동적 작업입니다.
        /// </param>
        /// <param name="boundaries">
        /// Exported package lifecycle boundaries.<br/>
        /// 제공할 패키지 생명주기 경계입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when an operation or boundary is <see langword="null"/>.<br/>
        /// 작업 또는 경계가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        public PlanFragment(IEnumerable<PlannedOperation> operations, IEnumerable<PackageOperationBoundary> boundaries)
        {
            Operations = Snapshots.List(operations);
            Boundaries = Snapshots.List(boundaries);
        }
    }

    /// <summary>
    /// Supplies an immutable fragment collection to registered composers.<br/>
    /// 등록된 합성기에 불변 계획 조각 컬렉션을 제공합니다.
    /// </summary>
    public sealed class PlanFragmentSet
    {
        /// <summary>
        /// Gets the immutable fragment snapshots supplied to a composer.<br/>
        /// 합성기에 제공하는 불변 계획 조각 스냅샷을 가져옵니다.
        /// </summary>
        public IReadOnlyList<PlanFragment> Fragments { get; }

        /// <summary>
        /// Snapshots fragments without executing or merging their operations.<br/>
        /// 작업을 실행하거나 병합하지 않고 계획 조각을 저장합니다.
        /// </summary>
        /// <param name="fragments">
        /// Passive plan fragments.<br/>
        /// 수동적 계획 조각입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="fragments"/> contains a <see langword="null"/> element.<br/>
        /// <paramref name="fragments"/>에 <see langword="null"/> 원소가 있으면 발생합니다.
        /// </exception>
        public PlanFragmentSet(IEnumerable<PlanFragment> fragments) => Fragments = Snapshots.List(fragments);
    }

    /// <summary>
    /// Exposes a validated passive plan, ordered operations, and observed execution preconditions.<br/>
    /// 검증된 수동적 계획, 순서가 정해진 작업 및 관측된 실행 사전 조건을 제공합니다.
    /// </summary>
    public sealed class InstallPlan
    {
        /// <summary>
        /// Gets the management owner to record for executed package changes.<br/>
        /// 실행한 패키지 변경에 기록할 관리 소유자를 가져옵니다.
        /// </summary>
        public string ManagementOwner { get; }
        /// <summary>
        /// Gets desired installation representations for post-execution verification.<br/>
        /// 실행 후 검증할 원하는 설치 표현을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<PackageId, PackageInstallation> DesiredInstallations { get; }
        /// <summary>
        /// Gets the resolved graph or package that the target must represent.<br/>
        /// 대상이 표현해야 하는 해석된 그래프 또는 패키지를 가져옵니다.
        /// </summary>
        public ResolvedPackageGraph Desired { get; }
        /// <summary>
        /// Gets the passive environment descriptor associated with these observations or operations.<br/>
        /// 이 관측 또는 작업과 연결된 수동적 환경 설명자를 가져옵니다.
        /// </summary>
        public InstallationTarget Target { get; }
        /// <summary>
        /// Gets immutable package transitions selected by the planner.<br/>
        /// 계획기가 선택한 불변 패키지 전이를 가져옵니다.
        /// </summary>
        public IReadOnlyList<PackageChange> Changes { get; }
        /// <summary>
        /// Gets passive operations; validated plans expose them in prerequisite order.<br/>
        /// 수동적 작업을 가져오며 검증된 계획은 선행 순서로 제공합니다.
        /// </summary>
        public IReadOnlyList<PlannedOperation> Operations { get; }
        /// <summary>
        /// Gets observed resource fingerprints that an execution runner must revalidate.<br/>
        /// 실행 관리자가 다시 검증해야 하는 관측된 리소스 지문을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, string> ResourcePreconditions { get; }

        internal InstallPlan(PlanningRequest request, IEnumerable<PackageChange> changes, IEnumerable<PlannedOperation> operations)
        {
            ManagementOwner = request.ManagementOwner;
            DesiredInstallations = request.Installations;
            Desired = request.Desired;
            Target = request.Current.Target;
            Changes = Snapshots.List(changes);
            Operations = Snapshots.List(operations);
            ResourcePreconditions = request.Current.ResourceFingerprints;
        }
    }
}
