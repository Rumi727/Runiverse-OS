#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Retrieves exact candidate identities and their dependency metadata for one source kind.<br/>
    /// 한 출처 종류의 정확한 후보 식별자와 의존성 메타데이터를 조회합니다.
    /// </summary>
    /// <remarks>
    /// Candidate identities must use the query's canonical source kind and key. Metadata must be complete for the candidate revision.<br/>
    /// 후보 식별자는 쿼리의 정규화된 출처 종류와 키를 사용해야 하며 메타데이터는 후보 리비전의 전체 정보를 포함해야 합니다.
    /// </remarks>
    public interface IPackageCatalogProvider
    {
        /// <summary>
        /// Gets the capability key for the canonical source.<br/>
        /// 정규화된 출처의 기능 키를 가져옵니다.
        /// </summary>
        string SourceKind { get; }

        /// <summary>
        /// Finds candidates in deterministic preference order without changing the installation target.<br/>
        /// 설치 대상을 변경하지 않고 결정적인 선호 순서로 후보를 검색합니다.
        /// </summary>
        /// <param name="query">
        /// The bound source and known requirements.<br/>
        /// 바인딩된 출처와 알려진 요구입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns candidate identities and diagnostics.<br/>
        /// 완료되면 후보 식별자와 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<CandidateSet>> FindCandidatesAsync(PackageQuery query, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves declarations from the exact candidate revision.<br/>
        /// 정확한 후보 리비전에서 선언을 조회합니다.
        /// </summary>
        /// <param name="candidate">
        /// The candidate selected for metadata retrieval.<br/>
        /// 메타데이터를 조회할 후보입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns dependency metadata and diagnostics.<br/>
        /// 완료되면 의존성 메타데이터와 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<PackageMetadata>> ReadMetadataAsync(PackageCandidate candidate, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Evaluates selection semantics independently of candidate discovery.<br/>
    /// 후보 검색과 독립적으로 선택 제약의 의미를 평가합니다.
    /// </summary>
    public interface IPackageConstraintEvaluator
    {
        /// <summary>
        /// Gets the routing key for the constraint semantics.<br/>
        /// 선택 제약 의미의 라우팅 키를 가져옵니다.
        /// </summary>
        string ConstraintKind { get; }
        /// <summary>
        /// Evaluates the constraint against an exact candidate without mutation.<br/>
        /// 변경 없이 정확한 후보에 대해 제약을 평가합니다.
        /// </summary>
        /// <param name="constraint">
        /// The immutable selection constraint.<br/>
        /// 불변 선택 제약입니다.
        /// </param>
        /// <param name="candidate">
        /// The exact candidate to evaluate.<br/>
        /// 평가할 정확한 후보입니다.
        /// </param>
        /// <returns>
        /// The acceptance decision and diagnostics.<br/>
        /// 허용 결정과 진단입니다.
        /// </returns>
        ConstraintMatch Evaluate(PackageConstraint constraint, PackageCandidate candidate);
    }

    /// <summary>
    /// Resolves the complete closure or returns diagnostics without an executable graph.<br/>
    /// 전체 의존성 집합을 해석하거나 실행 가능한 그래프 없이 진단을 반환합니다.
    /// </summary>
    public interface IPackageResolver
    {
        /// <summary>
        /// Resolves all retained roots into a complete graph or blocking diagnostics.<br/>
        /// 유지할 전체 루트를 완전한 그래프로 해석하거나 진행을 차단하는 진단을 반환합니다.
        /// </summary>
        /// <param name="request">
        /// Retained roots, source overrides, and a search budget.<br/>
        /// 유지할 루트, 출처 재지정 및 탐색 예산입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns a validated graph or diagnostics without a graph.<br/>
        /// 완료되면 검증된 그래프 또는 그래프 없는 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<ResolvedPackageGraph>> ResolveAsync(ResolutionRequest request, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Observes a target without mutating its package state.<br/>
    /// 패키지 상태를 변경하지 않고 대상을 관측합니다.
    /// </summary>
    public interface IInstalledStateProvider
    {
        /// <summary>
        /// Gets the routing key for the observed environment.<br/>
        /// 관측할 환경의 라우팅 키를 가져옵니다.
        /// </summary>
        string TargetKind { get; }
        /// <summary>
        /// Observes the effective target state without changing it.<br/>
        /// 대상을 변경하지 않고 실제 대상 상태를 관측합니다.
        /// </summary>
        /// <param name="target">
        /// The environment to observe.<br/>
        /// 관측할 환경입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns observations and diagnostics.<br/>
        /// 완료되면 관측과 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<InstalledEnvironmentSnapshot>> ReadAsync(InstallationTarget target, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Plans one installation kind from immutable inputs without target mutation or acquisition.<br/>
    /// 대상 변경이나 콘텐츠 획득 없이 불변 입력으로 한 설치 종류를 계획합니다.
    /// </summary>
    public interface IInstallationPlanningProvider
    {
        /// <summary>
        /// Gets the routing key for the installation mechanism.<br/>
        /// 설치 방법의 라우팅 키를 가져옵니다.
        /// </summary>
        string InstallationKind { get; }
        /// <summary>
        /// Contributes passive operations for the assigned transitions.<br/>
        /// 할당된 전이에 대한 수동적 작업을 제공합니다.
        /// </summary>
        /// <param name="context">
        /// Global snapshots and the assigned installation and retirement transitions.<br/>
        /// 전체 스냅샷과 할당된 설치 및 제거 전이입니다.
        /// </param>
        /// <returns>
        /// The passive fragment and planning diagnostics.<br/>
        /// 수동적 계획 조각과 계획 진단입니다.
        /// </returns>
        PackageResult<PlanFragment> Plan(InstallationPlanningContext context);
    }

    /// <summary>
    /// Combines shared environment edits before generic ordering and conflict validation.<br/>
    /// 공통 순서 및 충돌 검증 전에 공유 환경 변경을 합성합니다.
    /// </summary>
    /// <remarks>
    /// Rewrites operation references and package boundaries when merging operations. Must not mutate the target.<br/>
    /// 작업을 병합할 때 작업 참조와 패키지 경계를 갱신해야 하며 대상을 변경하면 안 됩니다.
    /// </remarks>
    public interface IPlanFragmentComposer
    {
        /// <summary>
        /// Gets the canonical location or configuration key used for descriptor comparison.<br/>
        /// 설명자 비교에 사용하는 정규화된 위치 또는 설정 키를 가져옵니다.
        /// </summary>
        string Key { get; }
        /// <summary>
        /// Merges shared edits while rewriting their operation and package references.<br/>
        /// 작업 및 패키지 참조를 갱신하면서 공유 변경을 병합합니다.
        /// </summary>
        /// <param name="request">
        /// The immutable planning input.<br/>
        /// 불변 계획 입력입니다.
        /// </param>
        /// <param name="fragments">
        /// The fragments to normalize.<br/>
        /// 정규화할 계획 조각입니다.
        /// </param>
        /// <returns>
        /// Normalized fragments and diagnostics.<br/>
        /// 정규화된 계획 조각과 진단입니다.
        /// </returns>
        PackageResult<PlanFragmentSet> Compose(PlanningRequest request, PlanFragmentSet fragments);
    }

    /// <summary>
    /// Acquires a requested content representation during execution, independently of installation.<br/>
    /// 설치와 독립적으로 실행 단계에서 요청된 콘텐츠 표현을 획득합니다.
    /// </summary>
    public interface IPackageContentProvider
    {
        /// <summary>
        /// Gets the routing key for the artifact acquired by this provider.<br/>
        /// 이 프로바이더가 획득할 콘텐츠 참조의 라우팅 키를 가져옵니다.
        /// </summary>
        string ArtifactKind { get; }
        /// <summary>
        /// Checks whether acquisition can produce the requested representation.<br/>
        /// 콘텐츠 획득이 요청된 표현을 생성할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="representation">
        /// The requested representation key.<br/>
        /// 요청된 표현 키입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when supported; otherwise, <see langword="false"/>.<br/>
        /// 지원하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        bool Supports(string representation);
        /// <summary>
        /// Obtains the requested representation during execution.<br/>
        /// 실행 단계에서 요청된 표현을 획득합니다.
        /// </summary>
        /// <param name="artifact">
        /// The exact immutable content reference.<br/>
        /// 정확한 불변 콘텐츠 참조입니다.
        /// </param>
        /// <param name="representation">
        /// The requested representation key.<br/>
        /// 요청된 표현 키입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns acquired content and diagnostics.<br/>
        /// 완료되면 획득한 콘텐츠와 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<AcquiredPackageContent>> AcquireAsync(PackageArtifactReference artifact,
            string representation, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Executes a registered operation payload using injected environment services.<br/>
    /// 주입된 환경 서비스로 등록된 작업 데이터를 실행합니다.
    /// </summary>
    public interface IInstallOperationExecutor
    {
        /// <summary>
        /// Gets the routing key for the operation executed by this provider.<br/>
        /// 이 프로바이더가 실행할 작업의 라우팅 키를 가져옵니다.
        /// </summary>
        string OperationKind { get; }
        /// <summary>
        /// Executes a matching payload using environment services owned by the implementation.<br/>
        /// 구현체가 소유한 환경 서비스로 해당 데이터를 실행합니다.
        /// </summary>
        /// <param name="operation">
        /// The immutable operation payload.<br/>
        /// 불변 작업 데이터입니다.
        /// </param>
        /// <param name="target">
        /// The environment identified by the plan.<br/>
        /// 계획이 지정한 환경입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns execution evidence and diagnostics.<br/>
        /// 완료되면 실행 증거와 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<OperationReceipt>> ExecuteAsync(InstallOperation operation,
            InstallationTarget target, CancellationToken cancellationToken);
    }

    /// <summary>
    /// Registers independently implemented capabilities in a host composition root.<br/>
    /// 호스트 구성 지점에서 독립적으로 구현한 기능을 등록합니다.
    /// </summary>
    public interface IPackageManagementExtension
    {
        /// <summary>
        /// Registers only the capabilities implemented by this extension.<br/>
        /// 이 확장에서 구현한 기능만 등록합니다.
        /// </summary>
        /// <param name="builder">
        /// The mutable registration collector.<br/>
        /// 가변 등록 수집기입니다.
        /// </param>
        void Register(PackageManagementBuilder builder);
    }

    /// <summary>
    /// Returns an acquired representation without prescribing a file-system layout.<br/>
    /// 파일 시스템 배치를 지정하지 않고 획득한 표현을 반환합니다.
    /// </summary>
    public abstract class AcquiredPackageContent
    {
        /// <summary>
        /// Gets the content representation produced by acquisition.<br/>
        /// 콘텐츠 획득으로 생성한 표현을 가져옵니다.
        /// </summary>
        public abstract string Representation { get; }
    }

    /// <summary>
    /// Captures execution evidence for adapter-defined verification and eventual recovery.<br/>
    /// 어댑터의 검증 및 향후 복구에 사용할 실행 증거를 저장합니다.
    /// </summary>
    public abstract class OperationReceipt { }
}
