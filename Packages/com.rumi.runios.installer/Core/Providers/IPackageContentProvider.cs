#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RuniOS.PackageManagement.Diagnostics;
using RuniOS.PackageManagement.Model;

namespace RuniOS.PackageManagement.Providers
{
    /// <summary>
    /// Acquires a requested content representation during execution, independently of installation.<br/>
    /// 설치와 독립적으로 실행 단계에서 요청된 콘텐츠 표현을 획득합니다.
    /// </summary>
    public interface IPackageContentProvider
    {
        /// <summary>
        /// Gets the routing key for the artifact acquired by this provider.<br/>
        /// 이 프로바이더가 획득할 콘텐츠 참조의 라우팅 키를 가져옵니다.
        /// </summary>
        string artifactKind { get; }
        /// <summary>
        /// Checks whether acquisition can produce the requested representation.<br/>
        /// 콘텐츠 획득이 요청된 표현을 생성할 수 있는지 확인합니다.
        /// </summary>
        /// <param name="representation">
        /// The requested representation key.<br/>
        /// 요청된 표현 키입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when supported; otherwise, <see langword="false"/>.<br/>
        /// 지원하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        bool Supports(string representation);
        /// <summary>
        /// Obtains the requested representation during execution.<br/>
        /// 실행 단계에서 요청된 표현을 획득합니다.
        /// </summary>
        /// <param name="artifact">
        /// The exact immutable content reference.<br/>
        /// 정확한 불변 콘텐츠 참조입니다.
        /// </param>
        /// <param name="representation">
        /// The requested representation key.<br/>
        /// 요청된 표현 키입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.<br/>
        /// 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When completed, returns acquired content and diagnostics.<br/>
        /// 완료되면 획득한 콘텐츠와 진단을 반환합니다.
        /// </returns>
        Task<PackageResult<AcquiredPackageContent>> AcquireAsync(PackageArtifactReference artifact,
            string representation, CancellationToken cancellationToken);
    }
}
