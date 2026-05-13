#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Linq.Async;
using System.IO;
using System.IO.Enumeration;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// OS의 실제 물리적 로컬 파일 시스템을 가상 파일 시스템 형태로 제공하는 구현체입니다.
    /// 지정된 타겟 디렉토리를 루트로 삼아 안전하게 파일 입출력을 수행합니다.
    /// </summary>
    public class PhysicalIOProvider(FilePath targetPath) : IWritableIOProvider
    {
        /// <inheritdoc/>
        public IOWriteNode rootNode => new IOWriteNode(this);

        /// <summary>
        /// 이 시스템이 가리키는 실제 OS 상의 디렉토리 전체 경로입니다. (샌드박스의 기준점)
        /// </summary>
        public FilePath targetPath { get; } = Path.GetFullPath(targetPath);

        /// <inheritdoc/>
        public bool isIndependent => false;

        /// <inheritdoc/>
        public IWritableIOProvider Recreate(FilePath path) => path.IsEmpty() ? this : new PhysicalIOProvider(targetPath + path);

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is PhysicalIOProvider otherPhysical && targetPath == otherPhysical.targetPath;

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(FilePath path, CancellationToken cancellationToken = default)
        {
            string fullPath = targetPath + path;
            var info = new FileInfo(fullPath);
            if (!info.Exists)
            {
                // 파일이 아니면 디렉토리인지 확인
                var dirInfo = new DirectoryInfo(fullPath);
                if (!dirInfo.Exists)
                    return UniTask.FromResult<IOEntry?>(null); // 둘 다 없으면 null

                return UniTask.FromResult<IOEntry?>(new IOEntry
                {
                    path = path,
                    metaData = new IOMetaData
                    {
                        name = dirInfo.Name,
                        creationTime = dirInfo.CreationTimeUtc,
                        lastAccessTime = dirInfo.LastAccessTimeUtc,
                        lastWriteTime = dirInfo.LastWriteTimeUtc,
                        attributes = dirInfo.Attributes
                    },
                    isDirectory = true
                });
            }

            return UniTask.FromResult<IOEntry?>(new IOEntry
            {
                path = path,
                metaData = new IOMetaData
                {
                    name = info.Name,
                    size = info.Length,
                    creationTime = info.CreationTimeUtc,
                    lastAccessTime = info.LastAccessTimeUtc,
                    lastWriteTime = info.LastWriteTimeUtc,
                    attributes = info.Attributes
                },
                isDirectory = false
            });
        }

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default)
        {
            var enumerable = new FileSystemEnumerable<IOEntry>
            (
                targetPath + path,
                (ref FileSystemEntry entry) =>
                {
                    FilePath entryFullPath = entry.ToFullPath().ToPath();
                    if (!entryFullPath.TryTrimStartPath(targetPath, out FilePath entryPath))
                    {
                        throw new InvalidOperationException
                        (
                            $"The enumerated file path '{entryFullPath}' is outside the bounds of the target directory '{targetPath}'. " +
                            "This may indicate an invalid symbolic link or a path traversal violation."
                        );
                    }

                    return new IOEntry
                    {
                        path = entryPath,
                        metaData = new IOMetaData
                        {
                            name = entry.FileName.ToString(),
                            size = entry.IsDirectory ? null : entry.Length,
                            creationTime = entry.CreationTimeUtc.UtcDateTime,
                            lastAccessTime = entry.LastAccessTimeUtc.UtcDateTime,
                            lastWriteTime = entry.LastWriteTimeUtc.UtcDateTime,
                            attributes = entry.Attributes
                        },
                        isDirectory = entry.IsDirectory
                    };
                },
                new EnumerationOptions
                {
                    RecurseSubdirectories = recursive,
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseSensitive,
                }
            );

            return enumerable.EnumerateOnThreadPool();
        }
        #endregion

        #region Read
        /// <inheritdoc cref="IIOProvider.OpenRead(FilePath, CancellationToken)"/>
        public FileStream OpenRead(FilePath path) => File.OpenRead(targetPath + path);
        UniTask<Stream> IIOProvider.OpenRead(FilePath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenRead(path));

        /// <inheritdoc/>
        public UniTask<byte[]> ReadAllBytes(FilePath path, CancellationToken cancellationToken = default) =>
            File.ReadAllBytesAsync(targetPath + path, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask<string> ReadAllText(FilePath path, CancellationToken cancellationToken = default) =>
            File.ReadAllTextAsync(targetPath + path, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLines(FilePath path, CancellationToken cancellationToken = default) =>
            File.ReadLines(targetPath + path).EnumerateOnThreadPool(cancellationToken: cancellationToken);
        #endregion

        #region Write
        public UniTask CreateDirectory(FilePath path, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(targetPath + path);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc cref="IWritableIOProvider.OpenWrite(FilePath, CancellationToken)"/>
        public FileStream OpenWrite(FilePath path) => File.OpenWrite(targetPath + path);
        UniTask<Stream> IWritableIOProvider.OpenWrite(FilePath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenWrite(path));

        /// <inheritdoc cref="IWritableIOProvider.CreateFile(FilePath, CancellationToken)"/>
        public FileStream CreateFile(FilePath path) => File.Create(targetPath + path);
        UniTask<Stream> IWritableIOProvider.CreateFile(FilePath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(CreateFile(path));

        /// <inheritdoc/>
        public UniTask WriteAllBytes(FilePath path, byte[] bytes, CancellationToken cancellationToken = default) =>
            File.WriteAllBytesAsync(targetPath + path, bytes, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask WriteAllText(FilePath path, string text, CancellationToken cancellationToken = default) =>
            File.WriteAllTextAsync(targetPath + path, text, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask WriteLines(FilePath path, IEnumerable<string> lines, CancellationToken cancellationToken = default) =>
            File.WriteAllLinesAsync(targetPath + path, lines, cancellationToken).AsUniTask();
        #endregion

        /// <inheritdoc/>
        public UniTask DeleteDirectory(FilePath path, CancellationToken cancellationToken = default)
        {
            Directory.Delete(targetPath + path, true);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask DeleteFile(FilePath path, CancellationToken cancellationToken = default)
        {
            File.Delete(targetPath + path);
            return UniTask.CompletedTask;
        }

        void IDisposable.Dispose() { }
    }
}