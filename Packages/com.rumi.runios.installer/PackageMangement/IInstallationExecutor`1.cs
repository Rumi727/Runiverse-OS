#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Executes typed descriptors and bridges the heterogeneous executor contract.<br/>
    /// 타입화된 descriptor를 실행하고 heterogeneous executor 계약을 연결합니다.
    /// </summary>
    /// <typeparam name="TInstallation">
    /// The supported descriptor type.<br/>
    /// 지원하는 descriptor 타입입니다.
    /// </typeparam>
    /// <remarks>
    /// Custom matching must preserve the typed bridge's accepted descriptor type.<br/>
    /// 사용자 matching은 typed bridge가 수락하는 descriptor 타입을 유지해야 합니다.
    /// </remarks>
    public interface IInstallationExecutor<in TInstallation> : IInstallationExecutor where TInstallation : IInstallation
    {
        /// <summary>
        /// Ensures typed requirements and produces one final result per input descriptor.<br/>
        /// 타입화된 요구사항을 만족시키고 입력 descriptor별 최종 결과를 생산합니다.
        /// </summary>
        /// <param name="installations">
        /// The supported descriptors.<br/>
        /// 지원하는 descriptor들입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel execution.<br/>
        /// 실행 취소에 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// Final results associated with the original input instances.<br/>
        /// 원래 입력 인스턴스에 대응하는 최종 결과를 반환합니다.
        /// </returns>
        IAsyncEnumerable<InstallationResult> EnsureAsync(IEnumerable<TInstallation> installations, CancellationToken cancellationToken = default);

        bool IInstallationExecutor.CanExecute(IInstallation installation) => installation is TInstallation;
        IAsyncEnumerable<InstallationResult> IInstallationExecutor.EnsureAsync(IEnumerable<IInstallation> installations, CancellationToken cancellationToken)
            => EnsureAsync(installations.Cast<TInstallation>(), cancellationToken);
    }
}
