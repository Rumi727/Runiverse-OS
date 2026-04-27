using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Linq.Async;
using System.IO;
using System.IO.Enumeration;
using System.Threading;

namespace RuniOS.IONew
{
    public class PhysicalIOProvider(FilePath targetPath) : IWritableIOProvider
    {
        public FilePath targetPath { get; } = targetPath;

        public bool isIndependent => false;

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

                return UniTask.FromResult<IOEntry?>(new IOEntry(
                    path,
                    new IOMetaData(dirInfo.Name, null, dirInfo.CreationTimeUtc, dirInfo.LastAccessTimeUtc, dirInfo.LastWriteTimeUtc, dirInfo.Attributes),
                    true
                ));
            }

            return UniTask.FromResult<IOEntry?>(new IOEntry(
                path: path,
                metaData: new IOMetaData(info.Name, info.Length, info.CreationTimeUtc, info.LastAccessTimeUtc, info.LastWriteTimeUtc, info.Attributes),
                isDirectory: false
            ));
        }

        public IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(FilePath path, bool recursive, CancellationToken cancellationToken = default)
        {
            var enumerable = new FileSystemEnumerable<IOEntry>(
                targetPath + path,
                (ref FileSystemEntry entry) => new IOEntry(
                    path + entry.FileName.ToString(),
                    new IOMetaData(
                        entry.FileName.ToString(),
                        entry.IsDirectory ? null : entry.Length,
                        entry.CreationTimeUtc.UtcDateTime,
                        entry.LastAccessTimeUtc.UtcDateTime,
                        entry.LastWriteTimeUtc.UtcDateTime,
                        entry.Attributes
                    ),
                    entry.IsDirectory
                ),
                new EnumerationOptions
                {
                    RecurseSubdirectories = recursive,
                    IgnoreInaccessible = true,
                    MatchCasing = MatchCasing.CaseSensitive,
                }
            );

            return enumerable.EnumerateOnThreadPool();
        }

        public FileStream OpenRead(FilePath path) => File.OpenRead(targetPath + path);
        UniTask<Stream> IIOProvider.OpenRead(FilePath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenRead(path));

        public FileStream OpenWrite(FilePath path) => File.OpenWrite(targetPath + path);
        UniTask<Stream> IWritableIOProvider.OpenWrite(FilePath path, CancellationToken cancellationToken) => UniTask.FromResult<Stream>(OpenWrite(path));

        public UniTask DirectoryDelete(FilePath path, CancellationToken cancellationToken = default)
        {
            Directory.Delete(path);
            return UniTask.CompletedTask;
        }

        public UniTask FileDelete(FilePath path, CancellationToken cancellationToken = default)
        {
            File.Delete(path);
            return UniTask.CompletedTask;
        }

        void IDisposable.Dispose() { }
    }
}