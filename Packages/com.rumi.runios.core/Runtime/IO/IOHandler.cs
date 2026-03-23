#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.Buffers;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace RuniOS.IO
{
    /// <summary>
    /// 파일 시스템 작업을 처리하는 추상 기본 클래스입니다.
    /// </summary>
    public static class IOEntry
    {
        /// <summary>
        /// 아무 작업도 수행하지 않는 빈 <see cref="IIOHandler"/> 인스턴스를 가져옵니다.
        /// </summary>
        public static readonly IIOHandler empty = new EmptyIOHandler();



        // 내부 캐스팅 헬퍼: 인터페이스 설계상 T 타입의 자식은 T 타입을 반환하므로 안전합니다.
        static T Cast<T>(IIOEntry entry) where T : IIOEntry => (T)entry;

        #region CreateChild Overloads
        /// <summary>
        /// 지정된 경로를 사용하여 자식 항목을 생성하고 원래의 핸들러 타입을 유지합니다.
        /// </summary>
        public static T CreateChild<T>(this T handler, FilePath path1, FilePath path2) where T : IIOEntry
            => Cast<T>(handler.CreateChild(path1).CreateChild(path2));

        /// <summary>
        /// 지정된 경로를 사용하여 자식 항목을 생성하고 원래의 핸들러 타입을 유지합니다.
        /// </summary>
        public static T CreateChild<T>(this T handler, FilePath path1, FilePath path2, FilePath path3) where T : IIOEntry
            => Cast<T>(handler.CreateChild(path1).CreateChild(path2).CreateChild(path3));

        /// <summary>
        /// 지정된 경로를 사용하여 자식 항목을 생성하고 원래의 핸들러 타입을 유지합니다.
        /// </summary>
        public static T CreateChild<T>(this T handler, FilePath path1, FilePath path2, FilePath path3, FilePath path4) where T : IIOEntry
            => Cast<T>(handler.CreateChild(path1).CreateChild(path2).CreateChild(path3).CreateChild(path4));

        /// <summary>
        /// 지정된 경로를 사용하여 자식 항목을 생성하고 원래의 핸들러 타입을 유지합니다.
        /// </summary>
        public static T CreateChild<T>(this T handler, FilePath path1, FilePath path2, FilePath path3, FilePath path4, FilePath path5) where T : IIOEntry
            => Cast<T>(handler.CreateChild(path1).CreateChild(path2).CreateChild(path3).CreateChild(path4).CreateChild(path5));

        /// <summary>
        /// 여러 경로 배열을 사용하여 자식 항목을 순차적으로 생성하고 원래의 핸들러 타입을 유지합니다.
        /// </summary>
        public static T CreateChild<T>(this T handler, params FilePath[] paths) where T : IIOEntry
            => handler.CreateChild((IEnumerable<FilePath>)paths);

        /// <summary>
        /// 경로 열거형을 사용하여 자식 항목을 순차적으로 생성하고 원래의 핸들러 타입을 유지합니다.
        /// </summary>
        public static T CreateChild<T>(this T handler, IEnumerable<FilePath> paths) where T : IIOEntry
            => Cast<T>(paths.Aggregate((IIOEntry)handler, (current, t) => current.CreateChild(t)));
        #endregion

        /// <summary>
        /// 지정된 와일드카드 패턴과 일치하는 파일이 존재하는지 확인하고, 존재하면 해당 타입의 핸들러를 반환합니다.
        /// </summary>
        public static async UniTask<T?> FileExists<T>(this T handler, WildcardPatterns wildcardPatterns) where T : class, IIOEntry
        {
            for (int i = 0; i < wildcardPatterns.count; i++)
            {
                T extensionHandler = Cast<T>(handler.AddExtension(wildcardPatterns[i]));
                if (await extensionHandler.FileExists())
                    return extensionHandler;
            }
            return null;
        }

        #region Handler Enumerables
        /// <summary>
        /// 현재 디렉터리 내의 모든 디렉터리 핸들러를 원래의 타입으로 가져옵니다.
        /// </summary>
        public static IUniTaskAsyncEnumerable<T> GetDirectoryHandlers<T>(this T handler) where T : IIOEntry
            => handler.GetDirectories().Select(x => Cast<T>(handler.CreateChild(x)));

        /// <summary>
        /// 모든 하위 디렉터리 핸들러를 원래의 타입으로 가져옵니다.
        /// </summary>
        public static IUniTaskAsyncEnumerable<T> GetAllDirectoryHandlers<T>(this T handler) where T : IIOEntry
            => handler.GetAllDirectories().Select(x => Cast<T>(handler.CreateChild(x)));

        /// <summary>
        /// 현재 디렉터리 내의 모든 파일 핸들러를 원래의 타입으로 가져옵니다.
        /// </summary>
        public static IUniTaskAsyncEnumerable<T> GetFileHandlers<T>(this T handler) where T : IIOEntry
            => handler.GetFiles().Select(x => Cast<T>(handler.CreateChild(x)));

        /// <summary>
        /// 와일드카드 패턴과 일치하는 현재 디렉터리의 파일 핸들러들을 원래의 타입으로 가져옵니다.
        /// </summary>
        public static IUniTaskAsyncEnumerable<T> GetFileHandlers<T>(this T handler, WildcardPatterns wildcardPatterns) where T : IIOEntry
            => handler.GetFiles(wildcardPatterns).Select(x => Cast<T>(handler.CreateChild(x)));

        /// <summary>
        /// 모든 하위 파일 핸들러를 원래의 타입으로 가져옵니다.
        /// </summary>
        public static IUniTaskAsyncEnumerable<T> GetAllFileHandlers<T>(this T handler) where T : IIOEntry
            => handler.GetAllFiles().Select(x => Cast<T>(handler.CreateChild(x)));

        /// <summary>
        /// 와일드카드 패턴과 일치하는 모든 하위 파일 핸들러를 원래의 타입으로 가져옵니다.
        /// </summary>
        public static IUniTaskAsyncEnumerable<T> GetAllFileHandlers<T>(this T handler, WildcardPatterns wildcardPatterns) where T : IIOEntry
            => handler.GetAllFiles(wildcardPatterns).Select(x => Cast<T>(handler.CreateChild(x)));
        #endregion



        /// <summary>
        /// 이 핸들러가 나타내는 파일의 MD5 해시 값을 계산합니다.
        /// </summary>
        /// <returns>파일의 MD5 해시를 포함하는 <see cref="byte"/>[]입니다.</returns>
        /// <exception cref="Exception">파일을 찾을 수 없거나(파일이 존재하지 않거나), 읽는 동안 오류가 발생한 경우입니다.</exception>
        public static async UniTask<string> GetFileChecksum(this IIOEntry entry)
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (entry is IPrecalculatedIOChecksum precalculated)
                return await precalculated.GetPrecalculatedChecksum();

            await using Stream stream = await entry.OpenRead();

            SynchronizationContext? callerContext = SynchronizationContext.Current;
            await UniTask.SwitchToThreadPool();

            using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192); // 8KB 버퍼 대여

            try
            {
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    incrementalHash.AppendData(buffer, 0, bytesRead);

                string result = BitConverter.ToString(incrementalHash.GetHashAndReset());
                if (callerContext != null && SynchronizationContext.Current != callerContext)
                    await UniTask.SwitchToSynchronizationContext(callerContext);

                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        sealed class EmptyIOHandler : IIOHandler
        {
            public EmptyIOHandler() => root = this;

            EmptyIOHandler(IIOHandler? parent, string name)
            {
                root = parent?.root ?? this;
                this.parent = parent;

                this.name = name;
                fullPath = parent?.fullPath + name;
            }

            public IIOHandler root { get; }
            IIOEntry IIOEntry.root => root;

            public IIOHandler? parent { get; }
            IIOEntry? IIOEntry.parent => parent;

            public bool isIndependent => true;

            public string name { get; } = string.Empty;

            public FilePath fullPath { get; } = new FilePath();

            public IIOHandler Recreate() => empty;
            IIOEntry IIOEntry.Recreate() => Recreate();

            public IIOHandler CreateChild(FilePath path) => new EmptyIOHandler(this, path);
            IIOEntry IIOEntry.CreateChild(FilePath path) => CreateChild(path);

            public IIOHandler AddExtension(FileExtension extension) => new EmptyIOHandler(parent, name + extension);
            IIOEntry IIOEntry.AddExtension(FileExtension extension) => AddExtension(extension);

            public UniTask<bool> DirectoryExists() => UniTask.FromResult(false);

            public UniTask<bool> FileExists() => UniTask.FromResult(false);

            public IUniTaskAsyncEnumerable<string> GetDirectories() => UniTaskAsyncEnumerable.Empty<string>();

            public IUniTaskAsyncEnumerable<FilePath> GetAllDirectories() => UniTaskAsyncEnumerable.Empty<FilePath>();

            public IUniTaskAsyncEnumerable<string> GetFiles() => UniTaskAsyncEnumerable.Empty<string>();
            public IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Empty<string>();
            public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData() => UniTaskAsyncEnumerable.Empty<FileMetaData>();
            public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Empty<FileMetaData>();

            public IUniTaskAsyncEnumerable<FilePath> GetAllFiles() => UniTaskAsyncEnumerable.Empty<FilePath>();
            public IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Empty<FilePath>();
            public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData() => UniTaskAsyncEnumerable.Empty<(FilePath relativePath, FileMetaData metaData)>();
            public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns) => UniTaskAsyncEnumerable.Empty<(FilePath relativePath, FileMetaData metaData)>();

            public UniTask<byte[]> ReadAllBytes() => UniTask.FromResult(Array.Empty<byte>());

            public UniTask<string> ReadAllText() => UniTask.FromResult(string.Empty);

            public IUniTaskAsyncEnumerable<string> ReadLines() => UniTaskAsyncEnumerable.Empty<string>();

            public UniTask<Stream> OpenRead() => UniTask.FromResult(Stream.Null);

            public UniTask WriteAllBytes(byte[] bytes) => UniTask.CompletedTask;

            public UniTask WriteAllText(string text) => UniTask.CompletedTask;

            public UniTask WriteLines(IEnumerable<string> lines) => UniTask.CompletedTask;

            public UniTask<Stream> OpenWrite() => UniTask.FromResult(Stream.Null);

            public UniTask<FileMetaData> GetFileMetaData() => UniTask.FromResult(new FileMetaData());
            public UniTask<string> GetFileChecksum() => UniTask.FromResult(string.Empty);

            // NaN != NaN과 비슷한 이유로 false를 반환하는게 더 맞을 듯 합니다.
            public bool IsSameTarget(IIOEntry? other) => false;
        }
    }
}