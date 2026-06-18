#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Linq.Async;
using RuniOS.Spans;
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
    public class PhysicalIOProvider : IWritableIOProvider
    {
        /// <summary>
        /// Initializes a new <see cref="PhysicalIOProvider"/> with the specified physical root and sandbox policy.<br/>
        /// 지정된 물리 루트와 샌드박스 정책으로 새 <see cref="PhysicalIOProvider"/>를 초기화합니다.
        /// </summary>
        /// <param name="targetPath">
        /// The physical root path exposed by this provider.<br/>
        /// 이 프로바이더가 노출하는 물리 루트 경로입니다.
        /// </param>
        /// <param name="sandboxPolicy">
        /// The sandbox policy applied before physical file-system access.<br/>
        /// 실제 파일 시스템 접근 전에 적용할 샌드박스 정책입니다.
        /// </param>
        public PhysicalIOProvider(PhysicalPath targetPath, SandboxPolicy sandboxPolicy = SandboxPolicy.Enabled)
        {
            this.targetPath = targetPath;
            this.sandboxPolicy = sandboxPolicy;
        }

        /// <inheritdoc/>
        public IOWriteNode rootNode => new IOWriteNode(this);

        /// <summary>
        /// Gets the physical root path used by this provider.<br/>
        /// 이 프로바이더가 기준으로 사용하는 물리 루트 경로를 가져옵니다.
        /// </summary>
        public PhysicalPath targetPath { get; }

        /// <summary>
        /// Gets the sandbox policy used before physical file-system access.<br/>
        /// 실제 파일 시스템 접근 전에 사용하는 샌드박스 정책을 가져옵니다.
        /// </summary>
        public SandboxPolicy sandboxPolicy { get; }

        /// <inheritdoc/>
        public bool isIndependent => false;

        /*/// <inheritdoc/>
        public IWritableIOProvider Recreate(RuniPath path) => path.IsEmpty() ? this : new PhysicalIOProvider(targetPath.Combine(path));*/

        /// <inheritdoc/>
        public bool IsSameTarget(IIOProvider other) => other is PhysicalIOProvider otherPhysical && targetPath == otherPhysical.targetPath && sandboxPolicy == otherPhysical.sandboxPolicy;

        /// <inheritdoc/>
        public UniTask<bool> DirectoryExists(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult(Directory.Exists(ResolveFullPath(path)));

        /// <inheritdoc/>
        public UniTask<bool> FileExists(RuniPath path, CancellationToken cancellationToken = default) => UniTask.FromResult(File.Exists(ResolveFullPath(path)));

        #region Entry
        /// <inheritdoc/>
        public UniTask<IOEntry?> GetEntry(RuniPath path, CancellationToken cancellationToken = default)
        {
            string fullPath = ResolveFullPath(path);
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
                    metaData = new FileMetaData
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
                metaData = new FileMetaData
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
                ResolveFullPath(path),
                (ref FileSystemEntry entry) =>
                {
                    PhysicalPath entryFullPath = (PhysicalPath)entry.ToFullPath();
                    ValidateEnumeratedPath(entryFullPath.value);

                    if (!entryFullPath.TryRemoveStartPath(targetPath, out RuniPath entryPath))
                        throw new InvalidOperationException($"The enumerated file path '{entryFullPath}' is outside the bounds of the target directory '{targetPath}'. ");

                    return new IOEntry
                    {
                        path = entryPath,
                        metaData = new FileMetaData
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
            )
            {
                ShouldRecursePredicate = (ref FileSystemEntry entry) =>
                {
                    ValidateEnumeratedPath(entry.ToFullPath());
                    return true;
                }
            };

            return enumerable.EnumerateOnThreadPool(cancellationToken: cancellationToken);
        }
        #endregion

        #region Read
        /// <inheritdoc cref="IIOProvider.OpenRead(RuniPath, CancellationToken)"/>
        public FileStream OpenRead(RuniPath path) => File.OpenRead(ResolveFullPath(path));
        UniTask<Stream> IIOProvider.OpenRead(RuniPath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenRead(path));

        /// <inheritdoc/>
        public UniTask<byte[]> ReadAllBytes(RuniPath path, CancellationToken cancellationToken = default) =>
            File.ReadAllBytesAsync(ResolveFullPath(path), cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask<string> ReadAllText(RuniPath path, CancellationToken cancellationToken = default) =>
            File.ReadAllTextAsync(ResolveFullPath(path), cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLines(RuniPath path, CancellationToken cancellationToken = default) =>
            File.ReadLines(ResolveFullPath(path)).EnumerateOnThreadPool(cancellationToken: cancellationToken);
        #endregion

        #region Write
        /// <inheritdoc/>
        public UniTask CreateDirectory(RuniPath path, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(ResolveFullPath(path));
            return UniTask.CompletedTask;
        }

        /// <inheritdoc cref="IWritableIOProvider.OpenWrite(RuniPath, CancellationToken)"/>
        public FileStream OpenWrite(RuniPath path) => File.OpenWrite(ResolveFullPath(path));
        UniTask<Stream> IWritableIOProvider.OpenWrite(RuniPath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenWrite(path));

        /// <inheritdoc cref="IWritableIOProvider.CreateFile(RuniPath, CancellationToken)"/>
        public FileStream CreateFile(RuniPath path) => File.Create(ResolveFullPath(path));
        UniTask<Stream> IWritableIOProvider.CreateFile(RuniPath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(CreateFile(path));

        /// <inheritdoc/>
        public UniTask WriteAllBytes(RuniPath path, byte[] bytes, CancellationToken cancellationToken = default) =>
            File.WriteAllBytesAsync(ResolveFullPath(path), bytes, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask WriteAllText(RuniPath path, string text, CancellationToken cancellationToken = default) =>
            File.WriteAllTextAsync(ResolveFullPath(path), text, cancellationToken).AsUniTask();

        /// <inheritdoc/>
        public UniTask WriteLines(RuniPath path, IEnumerable<string> lines, CancellationToken cancellationToken = default) =>
            File.WriteAllLinesAsync(ResolveFullPath(path), lines, cancellationToken).AsUniTask();
        #endregion

        /// <inheritdoc/>
        public UniTask DeleteDirectory(RuniPath path, CancellationToken cancellationToken = default)
        {
            Directory.Delete(ResolveFullPath(path), true);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask DeleteFile(RuniPath path, CancellationToken cancellationToken = default)
        {
            File.Delete(ResolveFullPath(path));
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// Combines <paramref name="path"/> with <see cref="targetPath"/> and validates the result against the configured sandbox policy.<br/>
        /// <paramref name="path"/>를 <see cref="targetPath"/>와 결합하고 설정된 샌드박스 정책으로 결과를 검증합니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative path to resolve.<br/>
        /// 해석할 프로바이더 기준 경로입니다.
        /// </param>
        /// <returns>
        /// The normalized physical path used for the file-system operation.<br/>
        /// 파일 시스템 작업에 사용할 정규화된 물리 경로를 반환합니다.
        /// </returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the resolved path violates the configured sandbox policy.<br/>
        /// 해석된 경로가 설정된 샌드박스 정책을 위반한 경우 발생합니다.
        /// </exception>
        string ResolveFullPath(RuniPath path)
        {
            string fullPath = targetPath.Combine(path).value;
            ValidateSandboxPolicy(path, fullPath);
            return fullPath;
        }

        /// <summary>
        /// Validates an enumerated physical path before it is returned or recursively traversed.<br/>
        /// 열거된 물리 경로가 반환되거나 재귀 탐색되기 전에 검증합니다.
        /// </summary>
        /// <param name="fullPath">
        /// The physical path reported by the file-system enumerator.<br/>
        /// 파일 시스템 열거자가 보고한 물리 경로입니다.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when <paramref name="fullPath"/> violates the configured sandbox policy.<br/>
        /// <paramref name="fullPath"/>가 설정된 샌드박스 정책을 위반한 경우 발생합니다.
        /// </exception>
        void ValidateEnumeratedPath(string fullPath) => ValidateSandboxPolicy(null, fullPath);

        /// <summary>
        /// Applies the configured sandbox policy to the specified path.<br/>
        /// 지정된 경로에 설정된 샌드박스 정책을 적용합니다.
        /// </summary>
        /// <param name="path">
        /// The provider-relative path before physical resolution, or <see langword="null"/> for enumerated paths.<br/>
        /// 물리 경로로 해석되기 전의 프로바이더 기준 경로이며, 열거된 경로이면 <see langword="null"/>입니다.
        /// </param>
        /// <param name="fullPath">
        /// The physical path to validate.<br/>
        /// 검증할 물리 경로입니다.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the path violates the configured sandbox policy.<br/>
        /// 경로가 설정된 샌드박스 정책을 위반한 경우 발생합니다.
        /// </exception>
        void ValidateSandboxPolicy(RuniPath? path, string fullPath)
        {
            if (sandboxPolicy == SandboxPolicy.Disabled)
                return;

            if (path.HasValue && ContainsTraversalSegment(path.Value))
                throw CreateTraversalSegmentException(path.Value);

            if (!IsSameOrChildPath(fullPath, targetPath.value))
                throw CreateSandboxException(fullPath, targetPath.value);

            if (ContainsReparsePoint(fullPath))
                throw CreateReparsePointException(fullPath);
        }

        static bool ContainsTraversalSegment(RuniPath path)
        {
            foreach (var segment in path.value.AsSpan().Split(RuniPath.directorySeparatorChar))
            {
                if (segment is "." || segment is "..")
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines whether an existing prefix of the specified normalized path contains a reparse point.<br/>
        /// 지정된 정규화 경로에서 존재하는 접두 경로가 재분석 지점을 포함하는지 확인합니다.
        /// </summary>
        /// <param name="fullPath">
        /// The normalized physical path to inspect.<br/>
        /// 검사할 정규화된 물리 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an existing segment has <see cref="FileAttributes.ReparsePoint"/>; otherwise, <see langword="false"/>.<br/>
        /// 존재하는 세그먼트가 <see cref="FileAttributes.ReparsePoint"/>를 가지면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        static bool ContainsReparsePoint(string fullPath)
        {
            string? root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
                return false;

            string currentPath = root;
            string relativePath = fullPath.Substring(root.Length);
            if (relativePath.Length == 0)
                return TryGetAttributes(currentPath, out FileAttributes rootAttributes) && (rootAttributes & FileAttributes.ReparsePoint) != 0;

            ReadOnlySpanSingleSplitter<char> segments = relativePath.AsSpan().Split(Path.DirectorySeparatorChar);
            foreach (var segment in segments)
            {
                currentPath = Path.Combine(currentPath, segment.ToString());
                if (!TryGetAttributes(currentPath, out FileAttributes attributes))
                    return false;

                if ((attributes & FileAttributes.ReparsePoint) != 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Tries to read file-system attributes for the specified physical path.<br/>
        /// 지정된 물리 경로의 파일 시스템 특성 읽기를 시도합니다.
        /// </summary>
        /// <param name="path">
        /// The physical path to inspect.<br/>
        /// 검사할 물리 경로입니다.
        /// </param>
        /// <param name="attributes">
        /// When this method returns <see langword="true"/>, contains the attributes read from <paramref name="path"/>.<br/>
        /// 이 메서드가 <see langword="true"/>를 반환하면 <paramref name="path"/>에서 읽은 특성을 포함합니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if attributes were read; <see langword="false"/> if the file or directory was not found.<br/>
        /// 특성을 읽었으면 <see langword="true"/>를 반환하고, 파일 또는 디렉터리를 찾지 못했으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        static bool TryGetAttributes(string path, out FileAttributes attributes)
        {
            try
            {
                attributes = File.GetAttributes(path);
                return true;
            }
            catch (FileNotFoundException)
            {
                attributes = default;
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                attributes = default;
                return false;
            }
        }

        /// <summary>
        /// Determines whether <paramref name="path"/> is equal to or under the normalized <paramref name="rootPath"/> on a segment boundary.<br/>
        /// <paramref name="path"/>가 정규화된 <paramref name="rootPath"/>와 같거나 세그먼트 경계 기준 하위 경로인지 확인합니다.
        /// </summary>
        /// <param name="path">
        /// The normalized physical path to test.<br/>
        /// 검사할 정규화된 물리 경로입니다.
        /// </param>
        /// <param name="rootPath">
        /// The normalized physical root path used as the boundary.<br/>
        /// 경계로 사용할 정규화된 물리 루트 경로입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="path"/> is equal to or under <paramref name="rootPath"/>; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="path"/>가 <paramref name="rootPath"/>와 같거나 그 아래에 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        static bool IsSameOrChildPath(string path, string rootPath)
        {
            StringComparison comparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            if (string.Equals(path, rootPath, comparison))
                return true;

            string? rootOfRootPath = Path.GetPathRoot(rootPath);
            if (!string.IsNullOrEmpty(rootOfRootPath) && string.Equals(rootPath, rootOfRootPath, comparison))
                return path.StartsWith(rootPath, comparison);

            if (path.Length <= rootPath.Length)
                return false;

            return path[rootPath.Length] == Path.DirectorySeparatorChar && path.StartsWith(rootPath, comparison);
        }

        /// <summary>
        /// Creates an exception for a path that escapes the provider root.<br/>
        /// 프로바이더 루트 밖으로 벗어난 경로에 대한 예외를 생성합니다.
        /// </summary>
        /// <param name="path">
        /// The offending physical path.<br/>
        /// 문제가 된 물리 경로입니다.
        /// </param>
        /// <param name="rootPath">
        /// The physical root path that should contain <paramref name="path"/>.<br/>
        /// <paramref name="path"/>를 포함해야 하는 물리 루트 경로입니다.
        /// </param>
        /// <returns>
        /// An exception describing the sandbox boundary violation.<br/>
        /// 샌드박스 경계 위반을 설명하는 예외를 반환합니다.
        /// </returns>
        static UnauthorizedAccessException CreateSandboxException(string path, string rootPath) => new UnauthorizedAccessException
        (
            $"The file path '{path}' is outside the bounds of the target directory '{rootPath}'. " +
            "This may indicate an invalid symbolic link, reparse point, or path traversal violation."
        );

        static UnauthorizedAccessException CreateTraversalSegmentException(RuniPath path) => new UnauthorizedAccessException
        (
            $"Path traversal segments are blocked by policy: '{path}'."
        );

        /// <summary>
        /// Creates an exception for a path blocked by the sandbox policy.<br/>
        /// 샌드박스 정책에 의해 차단된 경로에 대한 예외를 생성합니다.
        /// </summary>
        /// <param name="path">
        /// The physical path that contains a blocked reparse point.<br/>
        /// 차단된 재분석 지점을 포함한 물리 경로입니다.
        /// </param>
        /// <returns>
        /// An exception describing the blocked reparse-point access.<br/>
        /// 차단된 재분석 지점 접근을 설명하는 예외를 반환합니다.
        /// </returns>
        static UnauthorizedAccessException CreateReparsePointException(string path) => new UnauthorizedAccessException
        (
            $"Reparse point access is blocked by policy: '{path}'."
        );

        void IDisposable.Dispose() { }
    }
}
