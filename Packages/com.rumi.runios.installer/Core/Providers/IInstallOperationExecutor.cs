#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;
using RuniOS.PackageManagement.Planning;

namespace RuniOS.PackageManagement.Providers
{
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
        string operationKind { get; }
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
}
