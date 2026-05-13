#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    public abstract class VirtualFileBase : VirtualNode
    {
        // TODO : ReadAll*, WriteAll* 계열 추가

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> OpenRead(CancellationToken cancellationToken = default);

        /// <exception cref="ObjectDisposedException">
        /// 이 <see cref="VirtualDirectory"/> 인스턴스가 삭제되어 유효하지 않은 상태인 경우 발생합니다.
        /// </exception>
        public abstract UniTask<Stream> OpenWrite(CancellationToken cancellationToken = default);
    }
}