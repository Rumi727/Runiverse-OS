#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Spans;
using System.IO;

namespace RuniOS.IO
{
    /// <summary>
    /// 스트리밍 에셋에 대한 파일 및 디렉토리 작업에 대한 기능을 제공하는 핸들러입니다.
    /// <br/>
    /// 이 핸들러는 읽기 전용입니다.
    /// </summary>
    public partial class StreamingIOEntry : IIOEntry
    {
        public static readonly StreamingIOEntry instance = new StreamingIOEntry(null, string.Empty);
        
        /// <summary>
        /// 스트리밍 에셋 경로를 가져옵니다.
        /// </summary>
        public static FilePath streamingPath => Application.streamingAssetsPath;

        StreamingIOEntry(StreamingIOEntry? parent, string childPath)
        {
            root = parent?.root ?? this;
            this.parent = parent;

            name = childPath;
            fullPath = parent?.fullPath + childPath;
            
#if UNITY_ANDROID
            impl = new AndroidImpl(fullPath);
#else
            impl = new IOImpl(streamingPath + fullPath);
#endif
        }

        /// <inheritdoc cref="IIOEntry.root"/>
        public StreamingIOEntry root { get; }
        IIOEntry IIOEntry.root => root;

        /// <inheritdoc cref="IIOEntry.parent"/>
        public StreamingIOEntry? parent { get; }
        IIOEntry? IIOEntry.parent => parent;

        public bool isIndependent => false;

        public string name { get; } = string.Empty;

        public FilePath fullPath { get; } = FilePath.empty;

        readonly IImpl impl;

        #region Entry
        /// <summary>
        /// 현재 위치를 최상위 경로로 취급하는 새 <see cref="FileIOHandler"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>현재 위치를 기반으로 하는 새 <see cref="FileIOHandler"/> 인스턴스입니다.</returns>
        public StreamingIOEntry Recreate() => new StreamingIOEntry(null, fullPath);
        IIOEntry IIOEntry.Recreate() => Recreate();

        /// <inheritdoc cref="IIOEntry.CreateChild(FilePath)"/>
        public StreamingIOEntry CreateChild(FilePath path)
        {
            StreamingIOEntry entry = this;
            if (path.IsEmpty())
                return entry;

            foreach (var item in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
                entry = new StreamingIOEntry(entry, new string(item));

            return entry;
        }
        IIOEntry IIOEntry.CreateChild(FilePath path) => CreateChild(path);

        /// <inheritdoc cref="IIOEntry.AddExtension(FileExtension)"/>
        public StreamingIOEntry AddExtension(FileExtension extension) => new StreamingIOEntry(parent, name + extension);
        IIOEntry IIOEntry.AddExtension(FileExtension extension) => AddExtension(extension);
        #endregion

        #region Exists
        public UniTask<bool> DirectoryExists() => impl.DirectoryExists();
        public UniTask<bool> FileExists() => impl.FileExists();
        #endregion

        #region Get
        /// <inheritdoc cref="IIOEntry.GetDirectories()"/>
        public IUniTaskAsyncEnumerable<string> GetDirectories() => impl.GetDirectories();

        /// <inheritdoc cref="IIOEntry.GetAllDirectories()"/>
        public IUniTaskAsyncEnumerable<FilePath> GetAllDirectories() => impl.GetAllDirectories();

        /// <inheritdoc cref="IIOEntry.GetFiles()"/>
        public IUniTaskAsyncEnumerable<string> GetFiles() => impl.GetFiles();

        public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData() => impl.GetFilesWithMetaData();

        /// <inheritdoc cref="IIOEntry.GetFiles(WildcardPatterns)"/>
        public IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns) => impl.GetFiles(wildcardPatterns);

        public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns) => impl.GetFilesWithMetaData(wildcardPatterns);

        /// <inheritdoc cref="IIOEntry.GetAllFiles()"/>
        public IUniTaskAsyncEnumerable<FilePath> GetAllFiles() => impl.GetAllFiles();

        public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData() => impl.GetAllFilesWithMetaData();

        /// <inheritdoc cref="IIOEntry.GetAllFiles(WildcardPatterns)"/>
        public IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns) => impl.GetAllFiles(wildcardPatterns);

        public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns) => impl.GetAllFilesWithMetaData(wildcardPatterns);
        #endregion

        #region Read
        /// <inheritdoc cref="IIOEntry.ReadAllBytes()"/>
        public UniTask<byte[]> ReadAllBytes() => impl.ReadAllBytes();

        /// <inheritdoc cref="IIOEntry.ReadAllText()"/>
        public UniTask<string> ReadAllText() => impl.ReadAllText();

        /// <inheritdoc cref="IIOEntry.ReadLines()"/>
        public IUniTaskAsyncEnumerable<string> ReadLines() => impl.ReadLines();

        /// <inheritdoc cref="IIOEntry.OpenRead()"/>
        public UniTask<Stream> OpenRead() => impl.OpenRead();
        #endregion

        public UniTask<FileMetaData> GetFileMetaData() => impl.GetFileMetaData();

        public bool IsSameTarget(IIOEntry? other)
        {
            if (other is not StreamingIOEntry streamingIOEntry)
                return false;

            return fullPath == streamingIOEntry.fullPath;
        }
    }
}