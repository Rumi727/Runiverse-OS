#nullable enable
using System.Collections.Generic;
using System.Threading;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Optionally observes current requirements without executing them.<br/>
    /// 요구사항을 실행하지 않고 현재 상태를 선택적으로 관측합니다.
    /// </summary>
    public interface IInstallationPreviewer
    {
        /// <summary>
        /// Produces advisory observations that must not be reused as execution decisions.<br/>
        /// 실행 판단으로 재사용하면 안 되는 advisory 관측 결과를 생산합니다.
        /// </summary>
        /// <param name="installations">
        /// The descriptors matched by the executor.<br/>
        /// executor가 matching한 descriptor들입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel observation.<br/>
        /// 관측 취소에 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// One observation per original input descriptor.<br/>
        /// 원래 입력 descriptor별 관측 결과를 반환합니다.
        /// </returns>
        IAsyncEnumerable<InstallationPreview> PreviewAsync(IEnumerable<IInstallation> installations, CancellationToken cancellationToken = default);
    }
}
