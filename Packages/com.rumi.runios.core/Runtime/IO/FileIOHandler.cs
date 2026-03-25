#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Linq.Async;
using RuniOS.Spans;
using System.IO;

namespace RuniOS.IO
{
    /// <summary>
    /// 파일 시스템 경로를 처리하고 파일 및 디렉토리 작업에 대한 기능을 제공하는 핸들러입니다.
    /// </summary>
    public class FileIOHandler : IIOHandler
    {
        /// <summary>
        /// 지정된 대상 경로를 사용하여 <see cref="FileIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="targetPath">이 핸들러가 나타내는 파일 또는 디렉토리의 실제 경로입니다.</param>
        public FileIOHandler(FilePath targetPath)
        {
            root = this;
            this.targetPath = targetPath;
        }

        /// <summary>
        /// 부모 핸들러와 자식 경로를 사용하여 <see cref="FileIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="parent">이 핸들러의 부모 <see cref="FileIOHandler"/>입니다.</param>
        /// <param name="childPath">이 핸들러의 자식 경로입니다.</param>
        FileIOHandler(FileIOHandler? parent, string childPath)
        {
            root = parent?.root ?? this;
            this.parent = parent;

            name = childPath;
            fullPath = parent?.fullPath + childPath;

            targetPath = parent?.targetPath + childPath;
        }

        /// <inheritdoc cref="IIOEntry.root"/>
        public FileIOHandler root { get; }
        IIOHandler IIOHandler.root => root;
        IIOEntry IIOEntry.root => root;

        /// <inheritdoc cref="IIOEntry.parent"/>
        public FileIOHandler? parent { get; }
        IIOHandler? IIOHandler.parent => parent;
        IIOEntry? IIOEntry.parent => parent;

        public bool isIndependent => false;

        public string name { get; } = string.Empty;

        public FilePath fullPath { get; } = FilePath.empty;

        /// <summary>
        /// 이 핸들러가 나타내는 실제 파일 또는 디렉토리 경로를 가져옵니다.
        /// </summary>
        public FilePath targetPath { get; } = FilePath.empty;

        #region Entry
        /// <summary>
        /// 현재 위치를 최상위 경로로 취급하는 새 <see cref="FileIOHandler"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>현재 위치를 기반으로 하는 새 <see cref="FileIOHandler"/> 인스턴스입니다.</returns>
        public FileIOHandler Recreate() => new FileIOHandler(targetPath + fullPath);
        IIOHandler IIOHandler.Recreate() => Recreate();
        IIOEntry IIOEntry.Recreate() => Recreate();

        /// <inheritdoc cref="IIOEntry.CreateChild(FilePath)"/>
        public FileIOHandler CreateChild(FilePath path)
        {
            FileIOHandler handler = this;
            if (path.IsEmpty())
                return handler;

            foreach (var item in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
                handler = new FileIOHandler(handler, new string(item));

            return handler;
        }
        IIOHandler IIOHandler.CreateChild(FilePath path) => CreateChild(path);
        IIOEntry IIOEntry.CreateChild(FilePath path) => CreateChild(path);

        /// <inheritdoc cref="IIOEntry.AddExtension(FileExtension)"/>
        public FileIOHandler AddExtension(FileExtension extension) => new FileIOHandler(parent, name + extension);
        IIOHandler IIOHandler.AddExtension(FileExtension extension) => AddExtension(extension);
        IIOEntry IIOEntry.AddExtension(FileExtension extension) => AddExtension(extension);
        #endregion

        #region Exists
        public UniTask<bool> DirectoryExists() => UniTask.RunOnThreadPool(() => Directory.Exists(targetPath));
        public UniTask<bool> FileExists() => UniTask.RunOnThreadPool(() => File.Exists(targetPath));
        #endregion

        #region Get
        /// <inheritdoc cref="IIOEntry.GetDirectories()"/>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> GetDirectories() => UniTaskAsyncEnumerable.Create<string>(async (writer, cancellationToken) =>
        {
            var stream = Directory.EnumerateDirectories(targetPath)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var item in stream)
            {
                if (item.ToPath().TryTrimStartPath(targetPath, out FilePath result))
                    await writer.YieldAsync(result.ToString());
            }
        });

        /// <inheritdoc cref="IIOEntry.GetAllDirectories()"/>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<FilePath> GetAllDirectories() => UniTaskAsyncEnumerable.Create<FilePath>(async (writer, cancellationToken) =>
        {
            var stream = Directory.EnumerateDirectories(targetPath, "*", SearchOption.AllDirectories)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var item in stream)
                await writer.YieldAsync(item - targetPath);
        });

        /// <inheritdoc cref="IIOEntry.GetFiles()"/>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> GetFiles() => UniTaskAsyncEnumerable.Create<string>(async (writer, cancellationToken) =>
        {
            var stream = Directory.EnumerateFiles(targetPath)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var item in stream)
            {
                if (item.ToPath().TryTrimStartPath(targetPath, out FilePath result))
                    await writer.YieldAsync(result.ToString());
            }
        });

        public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData() => UniTaskAsyncEnumerable.Create<FileMetaData>(async (writer, cancellationToken) =>
        {
            var stream = new DirectoryInfo(targetPath).EnumerateFiles()
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var item in stream)
                await writer.YieldAsync(new FileMetaData(item.Name, item.Length, item.LastWriteTimeUtc));
        });

        /// <inheritdoc cref="IIOEntry.GetFiles(WildcardPatterns)"/>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Create<string>(async (writer, cancellationToken) =>
        {
            var stream = Directory.EnumerateFiles(targetPath)
                .Where(wildcardPatterns.IsMatch)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var item in stream)
            {
                if (item.ToPath().TryTrimStartPath(targetPath, out FilePath result))
                    await writer.YieldAsync(result.ToString());
            }
        });

        public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Create<FileMetaData>(async (writer, cancellationToken) =>
        {
            var stream = new DirectoryInfo(targetPath).EnumerateFiles()
                .Where(x => wildcardPatterns.IsMatch(x.FullName))
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var item in stream)
                await writer.YieldAsync(new FileMetaData(item.Name, item.Length, item.LastWriteTimeUtc));
        });

        /// <inheritdoc cref="IIOEntry.GetAllFiles()"/>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<FilePath> GetAllFiles() => UniTaskAsyncEnumerable.Create<FilePath>(async (writer, cancellationToken) =>
        {
            var stream = Directory.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var file in stream)
                await writer.YieldAsync(file - targetPath);
        });

        public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData() => UniTaskAsyncEnumerable.Create<(FilePath relativePath, FileMetaData metaData)>(async (writer, cancellationToken) =>
        {
            var stream = new DirectoryInfo(targetPath).EnumerateFiles("*", SearchOption.AllDirectories)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var file in stream)
                await writer.YieldAsync((file.FullName - targetPath, new FileMetaData(file.Name, file.Length, file.LastWriteTimeUtc)));
        });

        /// <inheritdoc cref="IIOEntry.GetAllFiles(WildcardPatterns)"/>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Create<FilePath>(async (writer, cancellationToken) =>
        {
            var stream = Directory.EnumerateFiles(targetPath, "*", SearchOption.AllDirectories)
                .Where(wildcardPatterns.IsMatch)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var file in stream)
                await writer.YieldAsync(file - targetPath);
        });

        public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Create<(FilePath relativePath, FileMetaData metaData)>(async (writer, cancellationToken) =>
        {
            var stream = new DirectoryInfo(targetPath).EnumerateFiles("*", SearchOption.AllDirectories)
                .Where(x => wildcardPatterns.IsMatch(x.FullName))
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var file in stream)
                await writer.YieldAsync((file.FullName - targetPath, new FileMetaData(file.Name, file.Length, file.LastWriteTimeUtc)));
        });
        #endregion

        #region Read
        /// <inheritdoc cref="IIOEntry.ReadAllBytes()"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public UniTask<byte[]> ReadAllBytes() => File.ReadAllBytesAsync(targetPath).AsUniTask();

        /// <inheritdoc cref="IIOEntry.ReadAllText()"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public UniTask<string> ReadAllText() => File.ReadAllTextAsync(targetPath).AsUniTask();

        /// <inheritdoc cref="IIOEntry.ReadLines()"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> ReadLines() => UniTaskAsyncEnumerable.Create<string>(async (writer, cancellationToken) =>
        {
            var stream = File.ReadLines(targetPath)
                .EnumerateOnThreadPool()
                .WithCancellation(cancellationToken);

            await foreach (var line in stream)
                await writer.YieldAsync(line);
        });

        /// <inheritdoc cref="IIOEntry.OpenRead()"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="FileNotFoundException">파일을 찾을 수 없는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="NotSupportedException">경로에 콜론(:)이 포함된 경우 발생합니다.</exception>
        public UniTask<Stream> OpenRead() => UniTask.FromResult<Stream>(new FileStream(targetPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true));
        #endregion

        #region Write
        /// <inheritdoc cref="IIOHandler.WriteAllBytes(byte[])"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우(읽기 전용 등) 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        public UniTask WriteAllBytes(byte[] bytes) => File.WriteAllBytesAsync(targetPath, bytes).AsUniTask();

        /// <inheritdoc cref="IIOHandler.WriteAllText(string)"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        public UniTask WriteAllText(string text) => File.WriteAllTextAsync(targetPath, text).AsUniTask();

        /// <inheritdoc cref="IIOHandler.WriteLines(IEnumerable{string})"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        public UniTask WriteLines(IEnumerable<string> lines) => File.WriteAllLinesAsync(targetPath, lines).AsUniTask();

        /// <inheritdoc cref="IIOHandler.OpenWrite()"/>
        /// <exception cref="ArgumentException">경로가 비어 있거나 공백만 포함하거나 유효하지 않은 문자를 포함하는 경우 발생합니다.</exception>
        /// <exception cref="IOException">I/O 오류가 발생한 경우 발생합니다.</exception>
        /// <exception cref="UnauthorizedAccessException">호출자에게 필요한 권한이 없는 경우 발생합니다.</exception>
        /// <exception cref="PathTooLongException">경로가 시스템 정의 최대 길이를 초과하는 경우 발생합니다.</exception>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 일부가 유효하지 않은 경우 발생합니다.</exception>
        public UniTask<Stream> OpenWrite() => UniTask.FromResult<Stream>(new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true));
        #endregion

        public UniTask<FileMetaData> GetFileMetaData()
        {
            FileInfo info = new FileInfo(targetPath);
            return UniTask.FromResult(new FileMetaData(name, info.Length, info.LastWriteTimeUtc));
        }

        public bool IsSameTarget(IIOEntry? other)
        {
            if (other is not FileIOHandler fileIOHandler)
                return false;

            return targetPath == fileIOHandler.targetPath;
        }
    }
}