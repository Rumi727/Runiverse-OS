#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Internal;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;

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
            IReadOnlyDictionary<string, IPackageCatalogProvider> catalogMap = Index(catalogs, x => x.sourceKind, diagnostics);
            IReadOnlyDictionary<string, IPackageConstraintEvaluator> evaluatorMap = Index(evaluators, x => x.constraintKind, diagnostics);
            IReadOnlyDictionary<string, IInstalledStateProvider> observationMap = Index(observations, x => x.targetKind, diagnostics);
            IReadOnlyDictionary<string, IInstallationPlanningProvider> plannerMap = Index(planners, x => x.installationKind, diagnostics);
            Index(composers, x => x.key, diagnostics);
            IReadOnlyDictionary<string, IPackageContentProvider> contentMap = Index(content, x => x.artifactKind, diagnostics);
            IReadOnlyDictionary<string, IInstallOperationExecutor> executorMap = Index(executors, x => x.operationKind, diagnostics);
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
}
