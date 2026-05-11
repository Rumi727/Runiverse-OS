#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// 메모리 기반 가상 파일 시스템(<see cref="VirtualDirectory"/>)을 <see cref="IWritableIOProvider"/> 형태로 제공하는 구현체입니다.
    /// </summary>
    public sealed class MemoryIOProvider : IWritableIOProvider
    {
        /// <summary>
        /// 지정된 가상 루트 디렉토리를 기반으로 <see cref="MemoryIOProvider"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="rootDirectory">이 프로바이더가 참조할 루트 가상 디렉토리입니다.</param>
        public MemoryIOProvider(VirtualDirectory rootDirectory) => this.rootDirectory = rootDirectory;

        /// <summary>
        /// 이 프로바이더가 참조하는 루트 가상 디렉토리입니다.
        /// </summary>
        public VirtualDirectory rootDirectory { get; }

        /// <inheritdoc/>
        public IOWriteNode rootNode => new IOWriteNode(this);

        /// <inheritdoc/>
        public bool isIndependent => rootDirectory.isIndependent;

        /// <inheritdoc/>
        public IWritableIOProvider Recreate(FilePath path)
        {
            if (path.IsEmpty())
                return this;

            VirtualDirectory? directory = rootDirectory.GetDirectory(path);
            if (directory == null)
                throw new DirectoryNotFoundException($"The directory at path '{path}' was not found.");

            return new MemoryIOProvider(directory);
        }

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is MemoryIOProvider otherMemory && rootDirectory == otherMemory.rootDirectory;

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(rootDirectory.GetEntry(path));
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<IOEntry>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            CancellationToken ct = linkedCTS.Token;

            foreach (IOEntry entry in rootDirectory.EnumerateEntries(path, recursive))
            {
                ct.ThrowIfCancellationRequested();
                await writer.YieldAsync(entry);
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

            VirtualFile? file = rootDirectory.GetFile(path);
            if (file == null)
                throw new FileNotFoundException($"The file at path '{path}' was not found.", path);

            return file.ReadAllBytesAsync(cancellationToken);
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

            VirtualFile? file = rootDirectory.GetFile(path);
            if (file == null)
                throw new FileNotFoundException($"The file at path '{path}' was not found.", path);

            return file.ReadAllTextAsync(cancellationToken);
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

            VirtualFile? file = rootDirectory.GetFile(path);
            if (file == null)
                throw new FileNotFoundException($"The file at path '{path}' was not found.", path);

            return file.ReadLines(cancellationToken);
        }

        /// <inheritdoc/>
        public UniTask<Stream> OpenRead(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFile? file = rootDirectory.GetFile(path);
            if (file == null)
                throw new FileNotFoundException($"The file at path '{path}' was not found.", path);

            return file.OpenRead(cancellationToken);
        }
        #endregion

        #region Write
        /// <inheritdoc/>
        public UniTask<Stream> OpenWrite(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return rootDirectory.OpenWrite(path);
        }

        /// <inheritdoc/>
        public UniTask WriteAllBytes(FilePath path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rootDirectory.FileWrite(path, new VirtualFile(bytes));
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask WriteAllText(FilePath path, string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rootDirectory.FileWrite(path, new VirtualFile(text));
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask WriteLines(FilePath path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rootDirectory.FileWrite(path, new VirtualFile(string.Join("\n", lines)));
            return UniTask.CompletedTask;
        }
        #endregion

        /// <inheritdoc/>
        public UniTask DirectoryDelete(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rootDirectory.DeleteDirectory(path);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask FileDelete(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            rootDirectory.DeleteFile(path);
            return UniTask.CompletedTask;
        }

        void IDisposable.Dispose() { }
    }
}
