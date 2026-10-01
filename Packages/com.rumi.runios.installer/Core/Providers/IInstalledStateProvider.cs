#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Providers
{
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
        string targetKind { get; }
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
}
