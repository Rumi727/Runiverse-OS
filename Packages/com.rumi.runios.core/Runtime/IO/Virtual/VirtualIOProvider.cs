#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO.Virtual
{
    /// <summary>
    /// 메모리 기반 가상 파일 시스템(<see cref="VirtualDirectory"/>)을 <see cref="IWritableIOProvider"/> 형태로 제공하는 구현체입니다.
    /// </summary>
    public sealed class VirtualIOProvider : IWritableIOProvider
    {
        /// <summary>
        /// 지정된 가상 루트 디렉토리를 기반으로 <see cref="VirtualIOProvider"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="rootDirectory">이 프로바이더가 참조할 루트 가상 디렉토리입니다.</param>
        public VirtualIOProvider(VirtualDirectoryBase rootDirectory) => this.rootDirectory = rootDirectory;

        /// <summary>
        /// 이 프로바이더가 참조하는 루트 가상 디렉토리입니다.
        /// </summary>
        public VirtualDirectoryBase rootDirectory { get; }

        /// <inheritdoc/>
        public IOWriteNode rootNode => new IOWriteNode(this);

        /// <inheritdoc/>
        public bool isIndependent => rootDirectory.isRoot;

        /// <inheritdoc/>
        public IWritableIOProvider Recreate(FilePath path)
        {
            if (path.IsEmpty())
                return this;

            if (rootDirectory.GetNode(path) is not VirtualDirectoryBase directory)
            {
                VirtualDirectoryBase.ThrowDirectoryNotFound(path);
                throw null;
            }

            return new VirtualIOProvider(directory);
        }

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is VirtualIOProvider otherVirtual && rootDirectory == otherVirtual.rootDirectory;

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default)
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
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<IOEntry>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            CancellationToken ct = linkedCTS.Token;

            IEnumerable<VirtualNode> enumerable = recursive ? rootDirectory.EnumerateNodes() : rootDirectory.EnumerateChildNodes();
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
        /// 지정된 경로의 파일의 모든 바이트를 읽습니다.
        /// </summary>
        /// <param name="path">읽을 파일의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 모든 바이트를 포함하는 <see cref="byte"/> 배열입니다.</returns>
        public UniTask<byte[]> ReadAllBytes(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetNode(path)?.AsFile();
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.ReadAllBytes(cancellationToken);
        }

        /// <summary>
        /// 지정된 경로의 파일의 모든 텍스트를 읽습니다.
        /// </summary>
        /// <param name="path">읽을 파일의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 모든 텍스트를 포함하는 <see cref="string"/>입니다.</returns>
        public UniTask<string> ReadAllText(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetNode(path)?.AsFile();
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.ReadAllText(cancellationToken);
        }

        /// <summary>
        /// 지정된 경로의 파일의 모든 줄을 한 줄씩 읽어 비동기 스트림으로 제공합니다.
        /// </summary>
        /// <param name="path">읽을 파일의 가상 파일 시스템 경로입니다.</param>
        /// <param name="cancellationToken">비동기 작업을 취소하는 데 사용되는 취소 토큰입니다.</param>
        /// <returns>파일의 각 줄을 제공하는 비동기 문자열 스트림입니다.</returns>
        public IUniTaskAsyncEnumerable<string> ReadLines(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetNode(path)?.AsFile();
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.ReadLines(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask<Stream> OpenRead(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFileBase? file = rootDirectory.GetNode(path)?.AsFile();
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);

            return file.OpenRead(cancellationToken);
        }
        #endregion

        #region Write
        /// <inheritdoc/>
        public UniTask<Stream> OpenWrite(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            VirtualFileBase file = rootDirectory.GetNode(path)?.AsFile() ?? new VirtualFile();
            return file.OpenWrite(cancellationToken);
        }
        
        /// <inheritdoc/>
        public UniTask<Stream> CreateFile(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            VirtualFileBase file = rootDirectory.GetNode(path)?.AsFile() ?? new VirtualFile();
            return file.Create(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask WriteAllBytes(FilePath path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            VirtualFileBase file = rootDirectory.GetNode(path)?.AsFile() ?? new VirtualFile();
            return file.WriteAllBytes(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask WriteAllText(FilePath path, string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            VirtualFileBase file = rootDirectory.GetNode(path)?.AsFile() ?? new VirtualFile();
            return file.WriteAllText(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask WriteLines(FilePath path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            VirtualFileBase file = rootDirectory.GetNode(path)?.AsFile() ?? new VirtualFile();
            return file.WriteLines(cancellationToken);
        }
        #endregion

        /// <inheritdoc/>
        public UniTask DeleteDirectory(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            VirtualDirectoryBase? directory = rootDirectory.GetNode(path)?.AsDirectory();
            if (directory == null)
                VirtualDirectoryBase.ThrowDirectoryNotFound(path);
                
            directory.Delete();
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask DeleteFile(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            VirtualFileBase? file = rootDirectory.GetNode(path)?.AsFile();
            if (file == null)
                VirtualDirectoryBase.ThrowFileNotFound(path);
                
            file.Delete();
            return UniTask.CompletedTask;
        }

        static string GetRelativePath(VirtualDirectoryBase directory, VirtualNode node)
        {
            node.ThrowIfNotAttachedException();
            return node.fullPath.Value.TrimStartPath(directory.fullPath ?? FilePath.empty);
        }

        void IDisposable.Dispose() { }
    }
}
