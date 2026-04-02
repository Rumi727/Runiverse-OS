#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;
using RuniOS.Spans;

namespace RuniOS.IO
{
    /// <summary>
    /// 가상 메모리 내의 파일 및 디렉토리 구조를 처리하는 핸들러입니다. 이 클래스는 상속될 수 없습니다.
    /// </summary>
    public sealed class MemoryIOHandler : IIOHandler
    {
        /// <summary>
        /// 지정된 가상 디렉토리를 사용하여 <see cref="MemoryIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="virtualDirectory">이 핸들러의 루트 가상 디렉토리입니다.</param>
        public MemoryIOHandler(VirtualDirectory virtualDirectory)
        {
            root = this;
            rootDirectory = virtualDirectory;
        }

        /// <summary>
        /// 루트 가상 디렉토리, 부모 핸들러 및 자식 경로를 사용하여 <see cref="MemoryIOHandler"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="rootDirectory">이 핸들러의 루트 가상 디렉토리입니다.</param>
        /// <param name="parent">이 핸들러의 부모 <see cref="MemoryIOHandler"/>입니다.</param>
        /// <param name="childPath">이 핸들러의 자식 경로입니다.</param>
        MemoryIOHandler(VirtualDirectory rootDirectory, MemoryIOHandler? parent, string childPath)
        {
            root = parent?.root ?? this;
            this.parent = parent;

            name = childPath;
            fullPath = parent?.fullPath + childPath;
            this.rootDirectory = rootDirectory;
        }

        /// <inheritdoc cref="IIOEntry.root"/>
        public MemoryIOHandler root { get; }
        IIOHandler IIOHandler.root => root;
        IIOEntry IIOEntry.root => root;

        /// <inheritdoc cref="IIOEntry.parent"/>
        public MemoryIOHandler? parent { get; }
        IIOHandler? IIOHandler.parent => parent;
        IIOEntry? IIOEntry.parent => parent;

        public bool isIndependent => rootDirectory.isIndependent;

        public string name { get; } = string.Empty;

        public FilePath fullPath { get; } = new FilePath();

        /// <summary>
        /// 이 핸들러의 루트 가상 디렉토리를 가져옵니다.
        /// </summary>
        readonly VirtualDirectory rootDirectory;

        #region Entry
        /// <summary>
        /// 현재 위치를 최상위 경로로 취급하는 새 <see cref="MemoryIOHandler"/> 인스턴스를 생성합니다.
        /// <br/>
        /// 주의: <see cref="VirtualDirectory"/>는 복제하지 않습니다.
        /// </summary>
        /// <returns>현재 위치를 기반으로 하는 새 <see cref="MemoryIOHandler"/> 인스턴스입니다.</returns>
        public MemoryIOHandler Recreate() => new MemoryIOHandler(rootDirectory.GetDirectory(fullPath) ?? new VirtualDirectory());
        IIOHandler IIOHandler.Recreate() => Recreate();
        IIOEntry IIOEntry.Recreate() => Recreate();

        /// <inheritdoc cref="IIOEntry.CreateChild(FilePath)"/>
        public MemoryIOHandler CreateChild(FilePath path)
        {
            MemoryIOHandler handler = this;
            if (path.IsEmpty())
                return handler;

            foreach (var item in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
            {
                string childPath = new string(item);
                handler = new MemoryIOHandler(rootDirectory, handler, childPath);
            }

            return handler;
        }
        IIOHandler IIOHandler.CreateChild(FilePath path) => CreateChild(path);
        IIOEntry IIOEntry.CreateChild(FilePath path) => CreateChild(path);

        /// <inheritdoc cref="IIOEntry.AddExtension(FileExtension)"/>
        public MemoryIOHandler AddExtension(FileExtension extension) => new MemoryIOHandler(rootDirectory, parent, name + extension);
        IIOHandler IIOHandler.AddExtension(FileExtension extension) => AddExtension(extension);
        IIOEntry IIOEntry.AddExtension(FileExtension extension) => AddExtension(extension);
        #endregion

        #region Exists
        public UniTask<bool> DirectoryExists() => UniTask.FromResult(!rootDirectory.isDeleted && rootDirectory.GetDirectory(fullPath) != null);
        public UniTask<bool> FileExists() => UniTask.FromResult(!rootDirectory.isDeleted && rootDirectory.GetFile(fullPath) != null);
        #endregion

        #region Get
        /// <inheritdoc cref="IIOEntry.GetDirectories()"/>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> GetDirectories() => rootDirectory.GetDirectories(fullPath).ToUniTaskAsyncEnumerable();

        /// <inheritdoc cref="IIOEntry.GetAllDirectories()"/>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<FilePath> GetAllDirectories() => rootDirectory.GetAllDirectories(fullPath)
            .Select(x => x - fullPath)
            .ToUniTaskAsyncEnumerable();

        /// <inheritdoc cref="IIOEntry.GetFiles()"/>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> GetFiles() => rootDirectory.GetFiles(fullPath).ToUniTaskAsyncEnumerable();

        public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData() => rootDirectory.GetFilesWithMetaData(fullPath).ToUniTaskAsyncEnumerable();

        /// <inheritdoc cref="IIOEntry.GetFiles(WildcardPatterns)"/>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns) => rootDirectory.GetFiles(fullPath)
            .Where(wildcardPatterns.IsMatch)
            .ToUniTaskAsyncEnumerable();

        public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns) => rootDirectory.GetFilesWithMetaData(fullPath)
            .Where(x => wildcardPatterns.IsMatch(x.name))
            .ToUniTaskAsyncEnumerable();

        /// <inheritdoc cref="IIOEntry.GetAllFiles()"/>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<FilePath> GetAllFiles() => rootDirectory.GetAllFiles(fullPath)
            .Select(x => x - fullPath)
            .ToUniTaskAsyncEnumerable();

        public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData() => rootDirectory.GetAllFilesWithMetaData(fullPath)
            .Select(x => (x.path - fullPath, x.metaData))
            .ToUniTaskAsyncEnumerable();

        /// <inheritdoc cref="IIOEntry.GetAllFiles(WildcardPatterns)"/>
        /// <exception cref="DirectoryNotFoundException">지정된 경로의 디렉토리를 찾을 수 없는 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns) => rootDirectory
            .GetAllFiles(fullPath)
            .Where(wildcardPatterns.IsMatch)
            .Select(x => x - fullPath)
            .ToUniTaskAsyncEnumerable();

        public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns) => rootDirectory.GetAllFilesWithMetaData(fullPath)
            .Where(x => wildcardPatterns.IsMatch(x.path))
            .Select(x => (x.path - fullPath, x.metaData))
            .ToUniTaskAsyncEnumerable();
        #endregion

        #region Read
        /// <inheritdoc cref="IIOEntry.ReadAllBytes()"/>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public UniTask<byte[]> ReadAllBytes() => rootDirectory.GetFile(fullPath)?.ReadAllBytesAsync() ?? throw new FileNotFoundException();

        /// <inheritdoc cref="IIOEntry.ReadAllText()"/>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public UniTask<string> ReadAllText() => rootDirectory.GetFile(fullPath)?.ReadAllTextAsync() ?? throw new FileNotFoundException();

        /// <inheritdoc cref="IIOEntry.ReadLines()"/>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public IUniTaskAsyncEnumerable<string> ReadLines() => rootDirectory.GetFile(fullPath)?.ReadLines() ?? throw new FileNotFoundException();

        /// <inheritdoc cref="IIOEntry.OpenRead()"/>
        /// <exception cref="FileNotFoundException">지정된 경로의 파일을 찾을 수 없는 경우 발생합니다.</exception>
        public UniTask<Stream> OpenRead() => rootDirectory.GetFile(fullPath)?.OpenRead() ?? throw new FileNotFoundException();
        #endregion

        #region Write
        public UniTask WriteAllBytes(byte[] bytes)
        {
            rootDirectory.FileWrite(fullPath, new VirtualFile(bytes));
            return UniTask.CompletedTask;
        }

        public UniTask WriteAllText(string text)
        {
            rootDirectory.FileWrite(fullPath, new VirtualFile(text));
            return UniTask.CompletedTask;
        }

        public UniTask WriteLines(IEnumerable<string> lines)
        {
            string content = string.Join("\n", lines);
            rootDirectory.FileWrite(fullPath, new VirtualFile(content));
            return UniTask.CompletedTask;
        }

        public UniTask<Stream> OpenWrite()
        {
            // 메모리 스트림을 생성하고, 스트림에 쓰기가 완료되어 파일로 등록되는 시점은 
            // 호출자가 제어해야 합니다. 여기서는 쓰기용 스트림을 반환합니다.
            return UniTask.FromResult<Stream>(new MemoryStream());
        }
        #endregion

        public UniTask DirectoryDelete()
        {
            rootDirectory.DeleteDirectory(fullPath);
            return UniTask.CompletedTask;
        }
        
        public UniTask FileDelete()
        {
            rootDirectory.DeleteFile(fullPath);
            return UniTask.CompletedTask;
        }

        public UniTask<FileMetaData> GetFileMetaData()
        {
            var file = rootDirectory.GetFile(fullPath);
            if (file?.metaData == null)
                throw new FileNotFoundException();

            return UniTask.FromResult(file.metaData.Value);
        }

        public bool IsSameTarget(IIOEntry? other)
        {
            if (other is not MemoryIOHandler memoryIOHandler)
                return false;

            return rootDirectory == memoryIOHandler.rootDirectory && fullPath == memoryIOHandler.fullPath;
        }
    }
}