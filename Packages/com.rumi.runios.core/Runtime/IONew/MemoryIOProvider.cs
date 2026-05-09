#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using System.IO;
using System.Threading;

namespace RuniOS.IONew
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

        /// <summary>
        /// 이 프로바이더의 최상위 루트를 가리키는 쓰기 가능한 노드를 가져옵니다.
        /// </summary>
        public IOWriteNode rootNode => new IOWriteNode(this);

        /// <inheritdoc/>
        public bool isIndependent => rootDirectory.isIndependent;

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (path.IsEmpty())
            {
                _ = rootDirectory.GetDirectory(path);
                return UniTask.FromResult<IOEntry?>(CreateDirectoryEntry(path));
            }

            VirtualDirectory? directory = rootDirectory.GetDirectory(path);
            if (directory != null)
                return UniTask.FromResult<IOEntry?>(CreateDirectoryEntry(path));

            VirtualFile? file = rootDirectory.GetFile(path);
            if (file != null)
                return UniTask.FromResult<IOEntry?>(CreateFileEntry(path, file));

            return UniTask.FromResult<IOEntry?>(null);
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default) => UniTaskAsyncEnumerable.Create<IOEntry>(async (writer, iterationToken) =>
        {
            using var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, iterationToken);
            CancellationToken ct = linkedCTS.Token;

            VirtualDirectory? startDirectory = rootDirectory.GetDirectory(path);
            if (startDirectory == null)
                throw new DirectoryNotFoundException($"The directory at path '{path}' was not found.");

            if (!recursive)
            {
                foreach (KeyValuePair<string, IVirtualNode> child in startDirectory.children)
                {
                    ct.ThrowIfCancellationRequested();
                    FilePath childPath = path + child.Key;
                    await writer.YieldAsync(CreateEntry(childPath, child.Value));
                }

                return;
            }

            Stack<(FilePath path, VirtualDirectory directory)> stack = new Stack<(FilePath path, VirtualDirectory directory)>();
            stack.Push((path, startDirectory));

            while (stack.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                (FilePath currentPath, VirtualDirectory currentDirectory) = stack.Pop();

                foreach (KeyValuePair<string, IVirtualNode> child in currentDirectory.children)
                {
                    ct.ThrowIfCancellationRequested();

                    FilePath childPath = currentPath + child.Key;
                    IOEntry entry = CreateEntry(childPath, child.Value);
                    await writer.YieldAsync(entry);

                    if (child.Value is VirtualDirectory childDirectory)
                        stack.Push((childPath, childDirectory));
                }
            }
        });
        #endregion

        #region Read
        /// <inheritdoc/>
        public UniTask<Stream> OpenRead(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            VirtualFile? file = rootDirectory.GetFile(path);
            if (file == null)
                throw new FileNotFoundException($"The file at path '{path}' was not found.", path);

            return file.OpenRead();
        }
        #endregion

        #region Write
        /// <inheritdoc/>
        public UniTask<Stream> OpenWrite(FilePath path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            FilePath parentPath = path.GetParentPath();
            if (rootDirectory.GetDirectory(parentPath) == null)
                throw new DirectoryNotFoundException($"The directory at path '{parentPath}' was not found.");

            return UniTask.FromResult<Stream>(new BufferedWriteStream(bytes => rootDirectory.FileWrite(path, new VirtualFile(bytes))));
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

        static IOEntry CreateEntry(FilePath path, IVirtualNode node) => node switch
        {
            VirtualDirectory => CreateDirectoryEntry(path),
            VirtualFile file => CreateFileEntry(path, file),
            _ => throw new InvalidDataException($"Unknown virtual node type '{node.GetType().Name}' at path '{path}'.")
        };

        static IOEntry CreateDirectoryEntry(FilePath path) => new IOEntry
        {
            path = path,
            metaData = new IOMetaData
            {
                name = path.GetFileName(),
                attributes = FileAttributes.Directory
            },
            isDirectory = true
        };

        static IOEntry CreateFileEntry(FilePath path, VirtualFile file)
        {
            FileMetaData fileMetaData = file.metaData ?? new FileMetaData(path.GetFileName(), 0, DateTime.MinValue);
            DateTime? lastWriteTime = fileMetaData.modifiedTime == DateTime.MinValue
                ? null
                : fileMetaData.modifiedTime.ToUniversalTime();

            return new IOEntry
            {
                path = path,
                metaData = new IOMetaData
                {
                    name = fileMetaData.name,
                    size = fileMetaData.size,
                    lastWriteTime = lastWriteTime,
                    attributes = FileAttributes.Normal
                },
                isDirectory = false
            };
        }

        sealed class BufferedWriteStream(Action<byte[]> onCommit) : MemoryStream
        {
            readonly Action<byte[]> _onCommit = onCommit;
            bool _isCommitted;

            protected override void Dispose(bool disposing)
            {
                if (disposing && !_isCommitted)
                {
                    _onCommit(ToArray());
                    _isCommitted = true;
                }

                base.Dispose(disposing);
            }
        }

        void IDisposable.Dispose() { }
    }
}
