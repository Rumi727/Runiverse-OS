#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Linq.Async;
using System.IO;
using System.IO.Enumeration;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// Exposes a physical local file-system directory through the <see cref="IWritableIOProvider"/> API.<br/>
    /// All provider-relative <see cref="RuniPath"/> values are resolved under <see cref="targetPath"/>.
    /// <br/><br/>
    /// 실제 로컬 파일 시스템 디렉터리를 <see cref="IWritableIOProvider"/> API로 제공합니다.<br/>
    /// 모든 프로바이더 기준 <see cref="RuniPath"/> 값은 <see cref="targetPath"/> 아래에서 해석됩니다.
    /// </summary>
    /// <remarks>
    /// TODO: 심볼릭 링크 및 기타 재분석 지점의 샌드박스 정책을 생성자 설정으로 추가해야 합니다:
    /// 원천 차단, 최종 해석 경로 검증, 추가 검사 없이 허용 중에서 선택할 수 있어야 합니다.
    /// </remarks>
    public class PhysicalIOProvider(PhysicalPath targetPath) : IWritableIOProvider
    {
        /// <inheritdoc/>
        public IOWriteNode rootNode => new IOWriteNode(this);

        /// <summary>
        /// Gets the physical root path used by this provider.<br/>
        /// 이 프로바이더가 기준으로 사용하는 물리 루트 경로를 가져옵니다.
        /// </summary>
        public PhysicalPath targetPath { get; } = targetPath;

        /// <inheritdoc/>
        public bool isIndependent => false;

        /*/// <inheritdoc/>
        public IWritableIOProvider Recreate(RuniPath path) => path.IsEmpty() ? this : new PhysicalIOProvider(targetPath.Combine(path));*/

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is PhysicalIOProvider otherPhysical && targetPath == otherPhysical.targetPath;

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default)
        {
            string fullPath = targetPath.Combine(path).value;
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
        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path, bool recursive, CancellationToken cancellationToken = default)
        {
            var enumerable = new FileSystemEnumerable<IOEntry>
            (
                targetPath.Combine(path).value,
                (ref FileSystemEntry entry) =>
                {
                    PhysicalPath entryFullPath = (PhysicalPath)entry.ToFullPath();
                    if (!entryFullPath.TryTrimStartPath(targetPath, out RuniPath entryPath))
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

            return enumerable.EnumerateOnThreadPool(cancellationToken: cancellationToken);
        }
        #endregion

        #region Read
        /// <inheritdoc cref="IIOProvider.OpenRead(RuniPath, CancellationToken)"/>
        public FileStream OpenRead(RuniPath path) => File.OpenRead(targetPath.Combine(path).value);
        UniTask<Stream> IIOProvider.OpenRead(RuniPath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenRead(path));

        /// <inheritdoc/>
        public UniTask<byte[]> ReadAllBytes(RuniPath path, CancellationToken cancellationToken = default) =>
            File.ReadAllBytesAsync(targetPath.Combine(path).value, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask<string> ReadAllText(RuniPath path, CancellationToken cancellationToken = default) =>
            File.ReadAllTextAsync(targetPath.Combine(path).value, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLines(RuniPath path, CancellationToken cancellationToken = default) =>
            File.ReadLines(targetPath.Combine(path).value).EnumerateOnThreadPool(cancellationToken: cancellationToken);
        #endregion

        #region Write
        /// <inheritdoc/>
        public UniTask CreateDirectory(RuniPath path, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(targetPath.Combine(path).value);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc cref="IWritableIOProvider.OpenWrite(RuniPath, CancellationToken)"/>
        public FileStream OpenWrite(RuniPath path) => File.OpenWrite(targetPath.Combine(path).value);
        UniTask<Stream> IWritableIOProvider.OpenWrite(RuniPath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenWrite(path));

        /// <inheritdoc cref="IWritableIOProvider.CreateFile(RuniPath, CancellationToken)"/>
        public FileStream CreateFile(RuniPath path) => File.Create(targetPath.Combine(path).value);
        UniTask<Stream> IWritableIOProvider.CreateFile(RuniPath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(CreateFile(path));

        /// <inheritdoc/>
        public UniTask WriteAllBytes(RuniPath path, byte[] bytes, CancellationToken cancellationToken = default) =>
            File.WriteAllBytesAsync(targetPath.Combine(path).value, bytes, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask WriteAllText(RuniPath path, string text, CancellationToken cancellationToken = default) =>
            File.WriteAllTextAsync(targetPath.Combine(path).value, text, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask WriteLines(RuniPath path, IEnumerable<string> lines, CancellationToken cancellationToken = default) =>
            File.WriteAllLinesAsync(targetPath.Combine(path).value, lines, cancellationToken).AsUniTask();
        #endregion

        /// <inheritdoc/>
        public UniTask DeleteDirectory(RuniPath path, CancellationToken cancellationToken = default)
        {
            Directory.Delete(targetPath.Combine(path).value, true);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask DeleteFile(RuniPath path, CancellationToken cancellationToken = default)
        {
            File.Delete(targetPath.Combine(path).value);
            return UniTask.CompletedTask;
        }

        void IDisposable.Dispose() { }
    }
}
