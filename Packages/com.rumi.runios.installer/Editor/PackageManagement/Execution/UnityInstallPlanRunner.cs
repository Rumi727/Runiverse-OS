#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Planning;
using RuniOS.PackageManagement.Providers;

namespace RuniOS.PackageManagement.Unity.Editor.Execution
{
    /// <summary>
    /// Executes a validated plan in prerequisite order through registered operation executors.<br/>
    /// 등록된 작업 실행기로 검증된 계획을 선행 순서에 따라 실행합니다.
    /// </summary>
    public sealed class UnityInstallPlanRunner
    {
        readonly PackageManagementServices services;
        /// <summary>
        /// Initializes the descriptor or provider using the supplied values.<br/>
        /// 전달한 값으로 설명자 또는 provider를 초기화합니다.
        /// </summary>
        /// <param name="services">
        /// The frozen executor/provider registry.<br/>
        /// 고정된 실행기 및 provider 레지스트리입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// May be thrown when a required argument is <see langword="null"/>.<br/>
        /// 필수 인수가 <see langword="null"/>이면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// May be thrown when input values or routing definitions are invalid.<br/>
        /// 입력 값 또는 연결 정의가 잘못되면 발생할 수 있습니다.
        /// </exception>
        public UnityInstallPlanRunner(PackageManagementServices services) => this.services = services ?? throw new ArgumentNullException(nameof(services));
        /// <summary>
        /// Executes the ordered plan through registered executors, retaining partial receipts on failure.<br/>
        /// 등록된 실행기로 계획을 순서대로 실행하고 실패 시 부분 receipt를 보존합니다.
        /// </summary>
        /// <param name="plan">
        /// The validated passive installation plan.<br/>
        /// 검증된 수동적 설치 계획입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns execution receipts and diagnostics.<br/>
        /// 완료되면 실행 receipt와 진단을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when the plan is <see langword="null"/>.<br/>
        /// 계획이 <see langword="null"/>이면 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// May be thrown when cancellation is requested before an operation completes.<br/>
        /// 작업 완료 전에 취소가 요청되면 발생할 수 있습니다.
        /// </exception>
        public async Task<PackageResult<InstallExecutionReport>> ExecuteAsync(InstallPlan plan, CancellationToken cancellationToken = default)
        {
            if (plan is null) throw new ArgumentNullException(nameof(plan));
            List<PackageDiagnostic> diagnostics = new(); List<OperationReceipt> receipts = new();
            // Resolve all capabilities before the first mutation.
            foreach (PlannedOperation operation in plan.operations)
                if (!services.operationExecutors.ContainsKey(operation.operation.kind))
                    diagnostics.Add(PackageDiagnostic.Error("unity:no-operation-executor", PackageDiagnosticPhase.Execution, $"No executor handles '{operation.operation.kind}'."));
            if (diagnostics.Count > 0) return new PackageResult<InstallExecutionReport>(new InstallExecutionReport(receipts), diagnostics);
            foreach (PlannedOperation operation in plan.operations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PackageResult<OperationReceipt> result = await services.operationExecutors[operation.operation.kind]
                    .ExecuteAsync(operation.operation, plan.target, cancellationToken).ConfigureAwait(false);
                diagnostics.AddRange(result.diagnostics);
                if (result.value is { } receipt) receipts.Add(receipt);
                if (!result.succeeded)
                {
                    if (!diagnostics.Any(x => x.severity == PackageDiagnosticSeverity.Error))
                        diagnostics.Add(PackageDiagnostic.Error("unity:missing-operation-receipt", PackageDiagnosticPhase.Execution, $"Executor '{operation.operation.kind}' returned no usable receipt."));
                    break;
                }
            }
            return new PackageResult<InstallExecutionReport>(new InstallExecutionReport(receipts), diagnostics);
        }
    }
}
