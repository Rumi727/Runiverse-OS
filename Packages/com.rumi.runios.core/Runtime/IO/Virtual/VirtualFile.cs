#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;

namespace RuniOS.IO.Virtual
{
    public class VirtualFile : VirtualFileBase
    {
        public VirtualFile() { }

        public VirtualFile(IONode shortcutNode) => this.shortcutNode = shortcutNode;
        
        readonly VirtualFileBuffer content = new VirtualFileBuffer();
        IONode? shortcutNode;

        public override UniTask<Stream> OpenRead()
        {
            if (shortcutNode != null)
                return shortcutNode.Value.file.OpenRead();

            return UniTask.FromResult<Stream>(new VirtualFileBufferStream(content, FileAccess.Read));
        }
        public override UniTask<Stream> OpenWrite()
        {
            // TODO: shortcutNode에서 원본 콘텐츠 복사
            
            return UniTask.FromResult<Stream>(new VirtualFileBufferStream(content, FileAccess.Write));
        }

        public override void OnDelete() => content.Clear();
    }
}