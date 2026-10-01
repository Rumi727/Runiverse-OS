#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Collects provider registrations and builds an independent registry snapshot.<br/>
    /// 프로바이더 등록을 수집하고 독립된 레지스트리 스냅샷을 생성합니다.
    /// </summary>
    public sealed class PackageManagementBuilder
    {
        readonly List<IPackageCatalogProvider> catalogs = new();
        readonly List<IPackageConstraintEvaluator> evaluators = new() { new ExactRevisionEvaluator() };
        readonly List<IInstalledStateProvider> observations = new();
        readonly List<IInstallationPlanningProvider> planners = new();
        readonly List<IPlanFragmentComposer> composers = new();
        readonly List<IPackageContentProvider> content = new();
        readonly List<IInstallOperationExecutor> executors = new();

        /// <summary>
        /// Registers a capability for candidate and metadata lookup.<br/>
        /// 후보 및 메타데이터 조회 기능을 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The capability provider to register.<br/>
        /// 등록할 기능 프로바이더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.<br/>
        /// <paramref name="provider"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddCatalog(IPackageCatalogProvider provider) => catalogs.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
        /// <summary>
        /// Registers a capability for constraint evaluation.<br/>
        /// 제약 평가 기능을 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The capability provider to register.<br/>
        /// 등록할 기능 프로바이더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.<br/>
        /// <paramref name="provider"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddConstraintEvaluator(IPackageConstraintEvaluator provider) => evaluators.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
        /// <summary>
        /// Registers a capability for read-only installed-state discovery.<br/>
        /// 읽기 전용 설치 상태 조회 기능을 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The capability provider to register.<br/>
        /// 등록할 기능 프로바이더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.<br/>
        /// <paramref name="provider"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddInstalledStateProvider(IInstalledStateProvider provider) => observations.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
        /// <summary>
        /// Registers a capability for passive installation planning.<br/>
        /// 수동적 설치 계획 기능을 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The capability provider to register.<br/>
        /// 등록할 기능 프로바이더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.<br/>
        /// <paramref name="provider"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddInstallationPlanner(IInstallationPlanningProvider provider) => planners.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
        /// <summary>
        /// Registers a capability for shared plan-fragment composition.<br/>
        /// 공유 계획 조각 합성 기능을 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The capability provider to register.<br/>
        /// 등록할 기능 프로바이더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.<br/>
        /// <paramref name="provider"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddPlanComposer(IPlanFragmentComposer provider) => composers.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
        /// <summary>
        /// Registers a capability for content acquisition.<br/>
        /// 콘텐츠 획득 기능을 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The capability provider to register.<br/>
        /// 등록할 기능 프로바이더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.<br/>
        /// <paramref name="provider"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddContentProvider(IPackageContentProvider provider) => content.Add(provider ?? throw new ArgumentNullException(nameof(provider)));
        /// <summary>
        /// Registers a capability for operation execution.<br/>
        /// 작업 실행 기능을 등록합니다.
        /// </summary>
        /// <param name="provider">
        /// The capability provider to register.<br/>
        /// 등록할 기능 프로바이더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="provider"/> is <see langword="null"/>.<br/>
        /// <paramref name="provider"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddOperationExecutor(IInstallOperationExecutor provider) => executors.Add(provider ?? throw new ArgumentNullException(nameof(provider)));

        /// <summary>
        /// Invokes an extension's registrations without performing discovery or installation.<br/>
        /// 검색이나 설치를 수행하지 않고 확장의 등록을 호출합니다.
        /// </summary>
        /// <param name="extension">
        /// The extension to register.<br/>
        /// 등록할 확장입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="extension"/> is <see langword="null"/>.<br/>
        /// <paramref name="extension"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void AddExtension(IPackageManagementExtension extension)
        {
            if (extension is null)
                throw new ArgumentNullException(nameof(extension));
            extension.Register(this);
        }

        /// <summary>
        /// Freezes registrations, rejecting duplicate capability keys with diagnostics.<br/>
        /// 등록을 스냅샷으로 고정하고 중복 기능 키를 진단으로 거부합니다.
        /// </summary>
        /// <returns>
        /// The registry snapshot, or diagnostics without a value when registrations conflict.<br/>
        /// 레지스트리 스냅샷을 반환하거나 등록이 충돌하면 값 없이 진단을 반환합니다.
        /// </returns>
        public PackageResult<PackageManagementServices> Build()
        {
            List<PackageDiagnostic> diagnostics = new();
            IReadOnlyDictionary<string, IPackageCatalogProvider> catalogMap = Index(catalogs, x => x.SourceKind, diagnostics);
            IReadOnlyDictionary<string, IPackageConstraintEvaluator> evaluatorMap = Index(evaluators, x => x.ConstraintKind, diagnostics);
            IReadOnlyDictionary<string, IInstalledStateProvider> observationMap = Index(observations, x => x.TargetKind, diagnostics);
            IReadOnlyDictionary<string, IInstallationPlanningProvider> plannerMap = Index(planners, x => x.InstallationKind, diagnostics);
            Index(composers, x => x.Key, diagnostics);
            IReadOnlyDictionary<string, IPackageContentProvider> contentMap = Index(content, x => x.ArtifactKind, diagnostics);
            IReadOnlyDictionary<string, IInstallOperationExecutor> executorMap = Index(executors, x => x.OperationKind, diagnostics);
            if (diagnostics.Count > 0)
                return new PackageResult<PackageManagementServices>(null, diagnostics);
            return new PackageResult<PackageManagementServices>(new PackageManagementServices(catalogMap, evaluatorMap,
                observationMap, plannerMap, Snapshots.List(composers), contentMap, executorMap));
        }

        static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> values, Func<T, string> key,
            List<PackageDiagnostic> diagnostics) where T : class
        {
            Dictionary<string, T> result = new(StringComparer.Ordinal);
            foreach (T value in values)
            {
                string providerKey = key(value);
                if (string.IsNullOrWhiteSpace(providerKey))
                    diagnostics.Add(PackageDiagnostic.Error("package:invalid-provider-key", PackageDiagnosticPhase.Registration,
                        $"A {typeof(T).Name} registration has an empty capability key."));
                else if (!result.TryAdd(providerKey, value))
                    diagnostics.Add(PackageDiagnostic.Error("package:duplicate-provider", PackageDiagnosticPhase.Registration,
                        $"Multiple {typeof(T).Name} registrations claim '{providerKey}'."));
            }
            return Snapshots.Map(result);
        }
    }

    /// <summary>
    /// Exposes frozen provider registrations without depending on a frontend or environment API.<br/>
    /// 프런트엔드나 환경 API에 의존하지 않고 고정된 프로바이더 등록을 제공합니다.
    /// </summary>
    public sealed class PackageManagementServices
    {
        internal IReadOnlyDictionary<string, IPackageCatalogProvider> Catalogs { get; }
        internal IReadOnlyDictionary<string, IPackageConstraintEvaluator> Evaluators { get; }
        internal IReadOnlyDictionary<string, IInstallationPlanningProvider> Planners { get; }
        internal IReadOnlyList<IPlanFragmentComposer> Composers { get; }
        readonly IReadOnlyDictionary<string, IInstalledStateProvider> observations;

        /// <summary>
        /// Gets content providers indexed by artifact kind for execution-time acquisition.<br/>
        /// 실행 단계의 획득에 사용할 콘텐츠 프로바이더를 아티팩트 종류별로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, IPackageContentProvider> ContentProviders { get; }
        /// <summary>
        /// Gets executors indexed by operation kind without invoking them.<br/>
        /// 실행기를 호출하지 않고 작업 종류별로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, IInstallOperationExecutor> OperationExecutors { get; }

        internal PackageManagementServices(IReadOnlyDictionary<string, IPackageCatalogProvider> catalogs,
            IReadOnlyDictionary<string, IPackageConstraintEvaluator> evaluators,
            IReadOnlyDictionary<string, IInstalledStateProvider> observations,
            IReadOnlyDictionary<string, IInstallationPlanningProvider> planners,
            IReadOnlyList<IPlanFragmentComposer> composers,
            IReadOnlyDictionary<string, IPackageContentProvider> content,
            IReadOnlyDictionary<string, IInstallOperationExecutor> executors)
        {
            Catalogs = catalogs;
            Evaluators = evaluators;
            this.observations = observations;
            Planners = planners;
            Composers = composers;
            ContentProviders = content;
            OperationExecutors = executors;
        }

        /// <summary>
        /// Creates an independent resolver using the frozen registry.<br/>
        /// 고정된 레지스트리로 독립적인 리졸버를 생성합니다.
        /// </summary>
        /// <returns>
        /// The resolver for this registry.<br/>
        /// 이 레지스트리의 리졸버입니다.
        /// </returns>
        public IPackageResolver CreateResolver() => new PackageResolver(this);
        /// <summary>
        /// Creates a passive planner using the frozen registry.<br/>
        /// 고정된 레지스트리로 수동적 계획기를 생성합니다.
        /// </summary>
        /// <returns>
        /// The planner for this registry.<br/>
        /// 이 레지스트리의 계획기입니다.
        /// </returns>
        public InstallPlanner CreatePlanner() => new(this);

        /// <summary>
        /// Dispatches read-only installed-state discovery by target kind.<br/>
        /// 대상 종류에 따라 읽기 전용 설치 상태 조회를 호출합니다.
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
        /// When completed, returns the observed snapshot or diagnostics.<br/>
        /// 완료되면 관측 스냅샷 또는 진단을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="target"/> is <see langword="null"/>.<br/>
        /// <paramref name="target"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when cancellation is requested.<br/>
        /// 취소가 요청되면 발생합니다.
        /// </exception>
        public async Task<PackageResult<InstalledEnvironmentSnapshot>> ReadInstalledStateAsync(InstallationTarget target,
            CancellationToken cancellationToken = default)
        {
            if (target is null)
                throw new ArgumentNullException(nameof(target));
            cancellationToken.ThrowIfCancellationRequested();
            if (observations.TryGetValue(target.Kind, out IInstalledStateProvider? provider) && provider is not null)
            {
                PackageResult<InstalledEnvironmentSnapshot> result = await provider.ReadAsync(target, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                InstalledEnvironmentSnapshot? snapshot = result.Value;
                if (!result.Succeeded || snapshot is null)
                {
                    if (result.Diagnostics.Any(x => x.Severity == PackageDiagnosticSeverity.Error))
                        return new PackageResult<InstalledEnvironmentSnapshot>(null, result.Diagnostics);
                    return new PackageResult<InstalledEnvironmentSnapshot>(null, result.Diagnostics.Concat(new[]
                    {
                        PackageDiagnostic.Error("package:missing-observation-result", PackageDiagnosticPhase.Observation,
                            "The installed-state provider returned no usable snapshot.")
                    }));
                }
                if (snapshot.Target.Kind != target.Kind || snapshot.Target.Key != target.Key)
                    return new PackageResult<InstalledEnvironmentSnapshot>(null, result.Diagnostics.Concat(new[]
                    {
                        PackageDiagnostic.Error("package:observation-target-mismatch", PackageDiagnosticPhase.Observation,
                            "The installed-state snapshot belongs to a different target.")
                    }));
                return result;
            }
            return new PackageResult<InstalledEnvironmentSnapshot>(null, new[]
            {
                PackageDiagnostic.Error("package:no-installed-state-provider", PackageDiagnosticPhase.Observation,
                    $"No installed-state provider handles '{target.Kind}'.")
            });
        }
    }

    sealed class ExactRevisionEvaluator : IPackageConstraintEvaluator
    {
        /// <summary>
        /// Gets the routing key for the constraint semantics.<br/>
        /// 선택 제약 의미의 라우팅 키를 가져옵니다.
        /// </summary>
        public string ConstraintKind => ExactRevisionConstraint.ConstraintKind;

        public ConstraintMatch Evaluate(PackageConstraint constraint, PackageCandidate candidate)
        {
            if (constraint is ExactRevisionConstraint exact)
                return new ConstraintMatch(exact.Revision == candidate.Identity.Revision);
            return new ConstraintMatch(false, new[]
            {
                PackageDiagnostic.Error("package:invalid-constraint-payload", PackageDiagnosticPhase.Resolution,
                    $"'{ConstraintKind}' requires an {nameof(ExactRevisionConstraint)} payload.")
            });
        }
    }
}
