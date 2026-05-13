#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    public class VirtualFile : VirtualFileBase
    {
        public VirtualFile() { }

        public VirtualFile(IONode shortcutNode) => this.shortcutNode = shortcutNode;

        readonly VirtualFileBuffer content = new VirtualFileBuffer();
        IONode? shortcutNode;

        public override UniTask<Stream> OpenRead(CancellationToken cancellationToken = default)
        {
            ThrowIfDeletedException();

            if (shortcutNode != null)
                return shortcutNode.Value.file.OpenRead(cancellationToken);

            return UniTask.FromResult<Stream>(new VirtualFileBufferStream(content, FileAccess.Read));
        }

        public override UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken = default)
        {
            if (shortcutNode != null)
                return shortcutNode.Value.file.ReadAllBytes(cancellationToken);

            return base.ReadAllBytes(cancellationToken);
        }

        public override UniTask<string> ReadAllText(CancellationToken cancellationToken = default)
        {
            if (shortcutNode != null)
                return shortcutNode.Value.file.ReadAllText(cancellationToken);

            return base.ReadAllText(cancellationToken);
        }

        public override IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken = default)
        {
            if (shortcutNode != null)
                return shortcutNode.Value.file.ReadLines(cancellationToken);

            return base.ReadLines(cancellationToken);
        }

        public override async UniTask<Stream> OpenWrite(CancellationToken cancellationToken = default)
        {
            ThrowIfDeletedException();

            Stream stream = new VirtualFileBufferStream(content, FileAccess.Write);

            if (shortcutNode != null)
            {
                await using Stream nodeStream = await shortcutNode.Value.file.OpenRead(cancellationToken);

                byte[] buffer = new byte[content.chunkSize];
                int readLength;

                while ((readLength = await nodeStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) != 0)
                    await stream.WriteAsync(buffer, 0, readLength, cancellationToken);

                stream.Seek(0, SeekOrigin.Begin);
                shortcutNode = null;
            }

            return stream;
        }

        public override UniTask<Stream> Create(CancellationToken cancellationToken = default)
        {
            ThrowIfDeletedException();

            Stream stream = new VirtualFileBufferStream(content, FileAccess.Write);
            stream.SetLength(0);

            shortcutNode = null;
            return UniTask.FromResult(stream);
        }

        public override void OnDelete() => content.Clear();
    }
}