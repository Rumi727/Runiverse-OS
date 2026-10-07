#nullable enable
using Cysharp.Threading.Tasks;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Represents an in-memory virtual file that can optionally start as a shortcut to another file node.<br/>
    /// 다른 파일 노드에 대한 바로가기로 시작할 수 있는 메모리 기반 가상 파일을 나타냅니다.
    /// </summary>
    public class VirtualFile : VirtualFileBase
    {
        /// <summary>
        /// Initializes a new empty <see cref="VirtualFile"/> instance.<br/>
        /// 비어 있는 새 <see cref="VirtualFile"/> 인스턴스를 초기화합니다.
        /// </summary>
        public VirtualFile() { }

        /// <summary>
        /// Initializes a new <see cref="VirtualFile"/> instance that reads from the specified shortcut node until it is written or created locally.<br/>
        /// 로컬에 쓰거나 새로 만들기 전까지 지정된 바로가기 노드에서 읽는 새 <see cref="VirtualFile"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="shortcutNode">
        /// The file node used as the initial read source.<br/>
        /// 초기 읽기 소스로 사용할 파일 노드입니다.
        /// </param>
        public VirtualFile(IONode shortcutNode) => this.shortcutNode = shortcutNode;

        readonly VirtualFileBuffer content = new VirtualFileBuffer();
        IONode? shortcutNode;

        /// <inheritdoc/>
        public override UniTask<Stream> OpenRead(CancellationToken cancellationToken = default)
        {
            ThrowIfDeletedException();

            if (shortcutNode != null)
                return shortcutNode.Value.file.OpenRead(cancellationToken);

            return UniTask.FromResult<Stream>(new VirtualFileBufferStream(content, FileAccess.Read));
        }

        /// <inheritdoc/>
        public override UniTask<byte[]> ReadAllBytes(CancellationToken cancellationToken = default)
        {
            if (shortcutNode != null)
                return shortcutNode.Value.file.ReadAllBytes(cancellationToken);

            return base.ReadAllBytes(cancellationToken);
        }

        /// <inheritdoc/>
        public override UniTask<string> ReadAllText(CancellationToken cancellationToken = default)
        {
            if (shortcutNode != null)
                return shortcutNode.Value.file.ReadAllText(cancellationToken);

            return base.ReadAllText(cancellationToken);
        }

        /// <inheritdoc/>
        public override IUniTaskAsyncEnumerable<string> ReadLines(CancellationToken cancellationToken = default)
        {
            if (shortcutNode != null)
                return shortcutNode.Value.file.ReadLines(cancellationToken);

            return base.ReadLines(cancellationToken);
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public override UniTask<Stream> Create(CancellationToken cancellationToken = default)
        {
            ThrowIfDeletedException();

            Stream stream = new VirtualFileBufferStream(content, FileAccess.Write);
            stream.SetLength(0);

            shortcutNode = null;
            return UniTask.FromResult(stream);
        }

        /// <inheritdoc/>
        public override void OnDelete() => content.Clear();
    }
}
