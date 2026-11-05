#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
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
        public PhysicalIOProvider(PhysicalPath targetPath, SandboxPolicy sandboxPolicy = SandboxPolicy.BlockLinks)
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

        /// <inheritdoc/>
        public bool isThreadSafe => true;

        /// <inheritdoc/>
        public bool supportsSymbolicLink => true;

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
                    ValidateSandboxPolicy(entryFullPath);

                    if (!entryFullPath.TryGetRelativePath(targetPath, out RuniPath entryPath))
                        throw CreateSandboxException(entryFullPath, targetPath);

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
                    MatchType = MatchType.Simple,
                    ReturnSpecialDirectories = false,
                    // 숨김·시스템 항목을 건너뛰는 기본값 대신 모든 항목을 반환하고, 숨길지는 사용하는 쪽이 정합니다.
                    AttributesToSkip = 0
                }
            );

            // 생각보다 I/O가 엄청 빠름 수만개 파일도 0.2초면 끝내버림
            // 하지만 ToUniTaskAsyncEnumerable()를 쓰니까 OneDrive 같은 실제 물리 장치에 마운트된게 아닐 때 렉 생겨서 몇 ms 밖에 안되는거 스레드 풀에 보내는게 나을듯
            return enumerable.EnumerateOnThreadPool();
        }
        #endregion

        #region Read
        // 비동기 파일 API는 스레드 풀을 거쳐, 메인 스레드에서 기다리면 호출마다 다음 업데이트까지 미뤄집니다.
        // 로컬 파일 하나를 읽고 쓰는 일은 그보다 훨씬 빠르므로 바로 처리합니다.

        /// <inheritdoc cref="IIOProvider.OpenRead(RuniPath, CancellationToken)"/>
        public FileStream OpenRead(RuniPath path) => File.OpenRead(ResolveFullPath(path));
        UniTask<Stream> IIOProvider.OpenRead(RuniPath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenRead(path));

        /// <inheritdoc/>
        public UniTask<byte[]> ReadAllBytes(RuniPath path, CancellationToken cancellationToken = default) =>
            UniTask.FromResult(File.ReadAllBytes(ResolveFullPath(path)));

        /// <inheritdoc/>
        public UniTask<string> ReadAllText(RuniPath path, CancellationToken cancellationToken = default) =>
            UniTask.FromResult(File.ReadAllText(ResolveFullPath(path)));

        /// <inheritdoc/>
        public IUniTaskAsyncEnumerable<string> ReadLines(RuniPath path, CancellationToken cancellationToken = default) =>
            File.ReadLines(ResolveFullPath(path)).ToUniTaskAsyncEnumerable();
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
        public UniTask WriteAllBytes(RuniPath path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            File.WriteAllBytes(ResolveFullPath(path), bytes);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask WriteAllText(RuniPath path, string text, CancellationToken cancellationToken = default)
        {
            File.WriteAllText(ResolveFullPath(path), text);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask WriteLines(RuniPath path, IEnumerable<string> lines, CancellationToken cancellationToken = default)
        {
            File.WriteAllLines(ResolveFullPath(path), lines);
            return UniTask.CompletedTask;
        }
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

        /// <inheritdoc/>
        public UniTask CopyFile(RuniPath sourcePath, RuniPath destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            string source = ResolveFullPath(sourcePath);
            string destination = ResolveFullPath(destinationPath);

            File.Copy(source, destination, overwrite);
            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask MoveFile(RuniPath sourcePath, RuniPath destinationPath, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            string source = ResolveFullPath(sourcePath);
            string destination = ResolveFullPath(destinationPath);

            File.Move(source, destination, overwrite);

            return UniTask.CompletedTask;
        }

        /// <inheritdoc/>
        public UniTask MoveDirectory(RuniPath sourcePath, RuniPath destinationPath, CancellationToken cancellationToken = default)
        {
            Directory.Move(ResolveFullPath(sourcePath), ResolveFullPath(destinationPath));
            return UniTask.CompletedTask;
        }

        /// <remarks>
        /// The link stores <paramref name="target"/> as a path relative to the directory containing the link, so it stays valid when that directory is moved together with its target.<br/>
        /// On Windows, creating a symbolic link requires administrator rights or Developer Mode.
        /// <br/><br/>
        /// 링크에는 <paramref name="target"/>을 링크가 있는 디렉터리 기준 상대 경로로 저장하므로, 그 디렉터리를 대상과 함께 옮겨도 링크가 유지됩니다.<br/>
        /// Windows에서 심볼릭 링크를 만들려면 관리자 권한이나 개발자 모드가 필요합니다.
        /// </remarks>
        /// <inheritdoc/>
        public UniTask CreateSymbolicLink(RuniPath path, RuniPath target, bool isDirectory, CancellationToken cancellationToken = default)
        {
            string linkPath = ResolveFullPath(path);
            string relativeTarget = Path.GetRelativePath(Path.GetDirectoryName(linkPath)!, ResolveFullPath(target));

            if (isDirectory)
                Directory.CreateSymbolicLink(linkPath, relativeTarget);
            else
                File.CreateSymbolicLink(linkPath, relativeTarget);

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
            PhysicalPath fullPath = targetPath / path;
            ValidateSandboxPolicy(fullPath);
            return fullPath.value;
        }

        /// <summary>
        /// Applies the configured sandbox policy to the specified path.<br/>
        /// 지정된 경로에 설정된 샌드박스 정책을 적용합니다.
        /// </summary>
        /// <param name="fullPath">
        /// The physical path to validate.<br/>
        /// 검증할 물리 경로입니다.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown when the path violates the configured sandbox policy.<br/>
        /// 경로가 설정된 샌드박스 정책을 위반한 경우 발생합니다.
        /// </exception>
        void ValidateSandboxPolicy(PhysicalPath fullPath)
        {
            if (sandboxPolicy == SandboxPolicy.Unrestricted)
                return;

            if (!fullPath.TryGetRelativePath(targetPath, out RuniPath relativePath))
                throw CreateSandboxException(fullPath, targetPath);

            string currentPath = targetPath.value;

            ReadOnlySpanSingleSplitter<char> segments = relativePath.GetSegmentsSpan();
            foreach (var segment in segments)
            {
                currentPath = Path.Combine(currentPath, segment.ToString());
                // 존재하지 않는 경로는 FileInfo.Attributes가 -1을 반환합니다.
                FileAttributes attributes = new FileInfo(currentPath).Attributes;
                if (attributes == (FileAttributes)(-1))
                    continue;

                if ((attributes & FileAttributes.ReparsePoint) == 0)
                    continue;

                if (sandboxPolicy == SandboxPolicy.BlockLinks)
                    throw CreateReparsePointException(fullPath);

                // 링크가 아닌 재분석 지점(OneDrive 자리 표시자 등)은 ResolveLinkTarget이 null을 반환하므로 그대로 허용합니다.
                FileSystemInfo? linkTarget = File.ResolveLinkTarget(currentPath, true);
                if (linkTarget != null && !((PhysicalPath)linkTarget.FullName).TryGetRelativePath(targetPath, out _))
                    throw CreateSandboxException((PhysicalPath)linkTarget.FullName, targetPath);
            }
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
        static UnauthorizedAccessException CreateSandboxException(PhysicalPath path, PhysicalPath rootPath) => new UnauthorizedAccessException
        (
            $"The file path '{path}' is outside the bounds of the target directory '{rootPath}'. " +
            "This may indicate an invalid symbolic link, reparse point, or path traversal violation."
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
        static UnauthorizedAccessException CreateReparsePointException(PhysicalPath path) => new UnauthorizedAccessException
        (
            $"Reparse point access is blocked by policy: '{path}'."
        );

        void IDisposable.Dispose() { }
    }
}
