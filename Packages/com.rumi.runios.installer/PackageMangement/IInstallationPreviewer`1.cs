#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Observes typed descriptors and bridges the optional heterogeneous preview contract.<br/>
    /// 타입화된 descriptor를 관측하고 선택적인 heterogeneous preview 계약을 연결합니다.
    /// </summary>
    /// <typeparam name="TInstallation">
    /// The descriptor type accepted by the matching executor.<br/>
    /// matching executor가 수락하는 descriptor 타입입니다.
    /// </typeparam>
    public interface IInstallationPreviewer<in TInstallation> : IInstallationPreviewer where TInstallation : IInstallation
    {
        /// <summary>
        /// Observes typed requirements without ensuring them.<br/>
        /// 타입화된 요구사항을 ensure하지 않고 관측합니다.
        /// </summary>
        /// <param name="installations">
        /// The supported descriptors.<br/>
        /// 지원하는 descriptor들입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel observation.<br/>
        /// 관측 취소에 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// Advisory observations associated with the original input instances.<br/>
        /// 원래 입력 인스턴스에 대응하는 advisory 관측 결과를 반환합니다.
        /// </returns>
        IAsyncEnumerable<InstallationPreview> PreviewAsync(IEnumerable<TInstallation> installations, CancellationToken cancellationToken = default);

        IAsyncEnumerable<InstallationPreview> IInstallationPreviewer.PreviewAsync(IEnumerable<IInstallation> installations, CancellationToken cancellationToken)
            => PreviewAsync(installations.Cast<TInstallation>(), cancellationToken);
    }
}
