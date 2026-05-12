#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    public abstract class VirtualFileBase : VirtualNode
    {
        // TODO : ReadAll*, WriteAll* 계열 추가
        
        public abstract UniTask<Stream> OpenRead(CancellationToken cancellationToken = default);
        public abstract UniTask<Stream> OpenWrite(CancellationToken cancellationToken = default);
    }
}