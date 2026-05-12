#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;

namespace RuniOS.IO.Virtual
{
    public abstract class VirtualFileBase : VirtualNode
    {
        public abstract UniTask<Stream> OpenRead();
        public abstract UniTask<Stream> OpenWrite();
    }
}