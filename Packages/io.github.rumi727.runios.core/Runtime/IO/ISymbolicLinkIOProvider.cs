#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// Provides symbolic link creation for a provider that supports it.<br/>
    /// 심볼릭 링크 생성을 지원하는 프로바이더에 심볼릭 링크 생성을 제공합니다.
    /// </summary>
    public interface ISymbolicLinkIOProvider : IIOProvider
    {
        /// <summary>
        /// Creates a symbolic link at the specified provider-relative path that points to another path in this provider.<br/>
        /// 지정된 프로바이더 기준 경로에, 이 프로바이더 안의 다른 경로를 가리키는 심볼릭 링크를 만듭니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative path of the symbolic link to create.<br/>
        /// 만들 심볼릭 링크의 프로바이더 기준 경로입니다.
        /// </param>
        /// <param name="target">
        /// The provider-relative path the symbolic link points to.<br/>
        /// 심볼릭 링크가 가리킬 프로바이더 기준 경로입니다.
        /// </param>
        /// <param name="isDirectory">
        /// <see langword="true"/> if <paramref name="target"/> is a directory; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="target"/>이 디렉터리이면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        UniTask CreateSymbolicLink(RuniPath path, RuniPath target, bool isDirectory, CancellationToken cancellationToken = default);
    }
}
