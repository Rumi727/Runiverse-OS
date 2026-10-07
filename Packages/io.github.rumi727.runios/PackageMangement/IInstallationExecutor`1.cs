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
        /// Ensures typed requirements and produces one final result per input installation entry.<br/>
        /// 타입화된 요구사항을 만족시키고 입력 installation 항목별 최종 결과 하나를 생산합니다.
        /// </summary>
        /// <param name="installations">
        /// The supported descriptors.<br/>
        /// 지원하는 descriptor들입니다.
        /// </param>
        /// <param name="force">
        /// Whether mismatched existing requirements may be replaced.<br/>
        /// 기존 요구사항과 다른 항목을 교체할 수 있는지 여부입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel execution.<br/>
        /// 실행 취소에 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// One final result per input installation entry.<br/>
        /// 입력 installation 항목별 최종 결과 하나를 반환합니다.
        /// </returns>
        IAsyncEnumerable<InstallationResult> EnsureAsync(IEnumerable<TInstallation> installations, bool force = false, CancellationToken cancellationToken = default);

        bool IInstallationExecutor.CanExecute(IInstallation installation) => installation is TInstallation;
        IAsyncEnumerable<InstallationResult> IInstallationExecutor.EnsureAsync(IEnumerable<IInstallation> installations, bool force, CancellationToken cancellationToken)
            => EnsureAsync(installations.Cast<TInstallation>(), force, cancellationToken);
    }
}
