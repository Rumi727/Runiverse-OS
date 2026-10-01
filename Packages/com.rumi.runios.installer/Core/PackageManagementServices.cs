#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Planning;
using RuniOS.PackageManagement.Providers;
using RuniOS.PackageManagement.Resolution;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Exposes frozen provider registrations without depending on a frontend or environment API.<br/>
    /// 프런트엔드나 환경 API에 의존하지 않고 고정된 프로바이더 등록을 제공합니다.
    /// </summary>
    public sealed class PackageManagementServices
    {
        internal IReadOnlyDictionary<string, IPackageCatalogProvider> catalogs { get; }
        internal IReadOnlyDictionary<string, IPackageConstraintEvaluator> evaluators { get; }
        internal IReadOnlyDictionary<string, IInstallationPlanningProvider> planners { get; }
        internal IReadOnlyList<IPlanFragmentComposer> composers { get; }
        readonly IReadOnlyDictionary<string, IInstalledStateProvider> observations;

        /// <summary>
        /// Gets content providers indexed by artifact kind for execution-time acquisition.<br/>
        /// 실행 단계의 획득에 사용할 콘텐츠 프로바이더를 아티팩트 종류별로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, IPackageContentProvider> contentProviders { get; }
        /// <summary>
        /// Gets executors indexed by operation kind without invoking them.<br/>
        /// 실행기를 호출하지 않고 작업 종류별로 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<string, IInstallOperationExecutor> operationExecutors { get; }

        internal PackageManagementServices(IReadOnlyDictionary<string, IPackageCatalogProvider> catalogs,
            IReadOnlyDictionary<string, IPackageConstraintEvaluator> evaluators,
            IReadOnlyDictionary<string, IInstalledStateProvider> observations,
            IReadOnlyDictionary<string, IInstallationPlanningProvider> planners,
            IReadOnlyList<IPlanFragmentComposer> composers,
            IReadOnlyDictionary<string, IPackageContentProvider> content,
            IReadOnlyDictionary<string, IInstallOperationExecutor> executors)
        {
            this.catalogs = catalogs;
            this.evaluators = evaluators;
            this.observations = observations;
            this.planners = planners;
            this.composers = composers;
            contentProviders = content;
            operationExecutors = executors;
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
            if (observations.TryGetValue(target.kind, out IInstalledStateProvider? provider) && provider is not null)
            {
                PackageResult<InstalledEnvironmentSnapshot> result = await provider.ReadAsync(target, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                InstalledEnvironmentSnapshot? snapshot = result.value;
                if (!result.succeeded || snapshot is null)
                {
                    if (result.diagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error))
                        return new PackageResult<InstalledEnvironmentSnapshot>(null, result.diagnostics);
                    return new PackageResult<InstalledEnvironmentSnapshot>(null, result.diagnostics.Concat(new[]
                    {
                        PackageDiagnostic.Error("package:missing-observation-result", PackageDiagnosticPhase.Observation,
                            "The installed-state provider returned no usable snapshot.")
                    }));
                }
                if (snapshot.target.kind != target.kind || snapshot.target.key != target.key)
                    return new PackageResult<InstalledEnvironmentSnapshot>(null, result.diagnostics.Concat(new[]
                    {
                        PackageDiagnostic.Error("package:observation-target-mismatch", PackageDiagnosticPhase.Observation,
                            "The installed-state snapshot belongs to a different target.")
                    }));
                return result;
            }
            return new PackageResult<InstalledEnvironmentSnapshot>(null, new[]
            {
                PackageDiagnostic.Error("package:no-installed-state-provider", PackageDiagnosticPhase.Observation,
                    $"No installed-state provider handles '{target.kind}'.")
            });
        }
    }
}
