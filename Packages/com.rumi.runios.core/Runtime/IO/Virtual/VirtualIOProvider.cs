#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// Exposes a virtual directory tree as a writable I/O provider.<br/>
    /// 가상 디렉터리 트리를 쓰기 가능한 I/O 프로바이더로 제공합니다.
    /// </summary>
    public sealed class VirtualIOProvider : IWritableIOProvider
    {
        /// <summary>
        /// Initializes a new <see cref="VirtualIOProvider"/> instance for the specified root directory.<br/>
        /// 지정된 루트 디렉터리를 사용하는 새 <see cref="VirtualIOProvider"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="rootDirectory">
        /// The virtual root directory referenced by this provider.<br/>
        /// 이 프로바이더가 참조하는 가상 루트 디렉터리입니다.
        /// </param>
        public VirtualIOProvider(VirtualDirectoryBase rootDirectory) => this.rootDirectory = rootDirectory;

        /// <summary>
        /// Gets the virtual root directory referenced by this provider.<br/>
        /// 이 프로바이더가 참조하는 가상 루트 디렉터리를 가져옵니다.
        /// </summary>
        public VirtualDirectoryBase rootDirectory { get; }

        /// <inheritdoc/>
        public IOWriteNode rootNode => new IOWriteNode(this);

        /// <inheritdoc/>
        public bool isIndependent => rootDirectory.isRoot;

        /*/// <inheritdoc/>
        public IWritableIOProvider Recreate(RuniPath path)
        {
            if (path.IsEmpty())
                return this;

            if (rootDirectory.GetNode(path) is not VirtualDirectoryBase directory)
            {
                VirtualDirectoryBase.ThrowDirectoryNotFound(path);
                throw null;
            }

            return new VirtualIOProvider(directory);
        }*/

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is VirtualIOProvider otherVirtual && rootDirectory == otherVirtual.rootDirectory;

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            VirtualNode? node = rootDirectory.GetNode(path);
            if (node == null)
                return UniTask.FromResult<IOEntry?>(null);

            return UniTask.FromResult<IOEntry?>(new IOEntry
            {
                path = path,
                metaData = node.metaData,
                isDirectory = node.isDirectory
            });
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<IOEntry>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            CancellationToken ct = linkedCTS.Token;

            VirtualDirectoryBase? directory = rootDirectory.GetDirectory(path);
            if (directory == null)
                return;

            IEnumerable<VirtualNode> enumerable = recursive ? directory.EnumerateNodes() : directory.EnumerateChildNodes();
            foreach (VirtualNode node in enumerable)
            {
                ct.ThrowIfCancellationRequested();
                await writer.YieldAsync(new IOEntry
                {
                    path = GetRelativePath(rootDirectory, node),
                    metaData = node.metaData,
                    isDirectory = node.isDirectory
                });
            }
        });
        #endregion

        #region Read
        /// <summary>
        /// Reads all bytes from the file at the specified path.<br/>
        /// 지정된 경로의 파일에서 모든 바이트를 읽습니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to read.<br/>
        /// 읽을 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns a byte array containing the full file contents.<br/>
        /// 비동기 작업이 완료되면 파일 전체 내용을 포함하는 <see cref="byte"/> 배열을 반환합니다.
        /// </returns>
        public UniTask<byte[]> ReadAllBytes(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetFile(path);
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.ReadAllBytes(cancellationToken);
        }

        /// <summary>
        /// Reads all text from the file at the specified path.<br/>
        /// 지정된 경로의 파일에서 모든 텍스트를 읽습니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to read.<br/>
        /// 읽을 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the full file contents as text.<br/>
        /// 비동기 작업이 완료되면 파일 전체 내용을 텍스트로 반환합니다.
        /// </returns>
        public UniTask<string> ReadAllText(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetFile(path);
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.ReadAllText(cancellationToken);
        }

        /// <summary>
        /// Reads the file at the specified path as an asynchronous sequence of lines.<br/>
        /// 지정된 경로의 파일을 줄 단위 비동기 시퀀스로 읽습니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative file path to read.<br/>
        /// 읽을 프로바이더 기준 파일 경로입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// An asynchronous sequence that yields each line from the file.<br/>
        /// 파일의 각 줄을 제공하는 비동기 시퀀스입니다.
        /// </returns>
        public IUniTaskAsyncEnumerable<string> ReadLines(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetFile(path);
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.ReadLines(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask<Stream> OpenRead(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetFile(path);
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.OpenRead(cancellationToken);
        }
        #endregion

        #region Write
        /// <inheritdoc/>
        public UniTask CreateDirectory(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rootDirectory.CreateDirectory(path);

            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask<Stream> OpenWrite(RuniPath path, CancellationToken cancellationToken = default)
        {
            VirtualNode.ThrowIfInvalidFileName(path.GetFileName());

            cancellationToken.ThrowIfCancellationRequested();
            return rootDirectory.GetOrCreateFile(path).OpenWrite(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask<Stream> CreateFile(RuniPath path, CancellationToken cancellationToken = default)
        {
            VirtualNode.ThrowIfInvalidFileName(path.GetFileName());

            cancellationToken.ThrowIfCancellationRequested();
            return rootDirectory.GetOrCreateFile(path).Create(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask WriteAllBytes(RuniPath path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            VirtualNode.ThrowIfInvalidFileName(path.GetFileName());

            cancellationToken.ThrowIfCancellationRequested();
            return rootDirectory.GetOrCreateFile(path).WriteAllBytes(bytes, cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask WriteAllText(RuniPath path, string text, CancellationToken cancellationToken = default)
        {
            VirtualNode.ThrowIfInvalidFileName(path.GetFileName());

            cancellationToken.ThrowIfCancellationRequested();
            return rootDirectory.GetOrCreateFile(path).WriteAllText(text, cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask WriteLines(RuniPath path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            VirtualNode.ThrowIfInvalidFileName(path.GetFileName());

            cancellationToken.ThrowIfCancellationRequested();
            return rootDirectory.GetOrCreateFile(path).WriteLines(lines, cancellationToken);
        }
        #endregion

        /// <inheritdoc/>
        public UniTask DeleteDirectory(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualDirectoryBase? directory = rootDirectory.GetNode(path)?.AsDirectory();
            if (directory == null)
                VirtualDirectoryBase.ThrowDirectoryNotFound(path);

            directory.Delete();
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask DeleteFile(RuniPath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetNode(path)?.AsFile();
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            file.Delete();
            return UniTask.CompletedTask;
        }

        static RuniPath GetRelativePath(VirtualDirectoryBase directory, VirtualNode node)
        {
            node.ThrowIfNotAttachedException();
            return node.fullPath.Value.RemoveStartPath(directory.fullPath ?? RuniPath.empty);
        }

        void IDisposable.Dispose() { }
    }
}
