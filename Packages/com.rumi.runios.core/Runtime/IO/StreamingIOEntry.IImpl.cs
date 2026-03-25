using Cysharp.Threading.Tasks;
using System.IO;

namespace RuniOS.IO
{
    public partial class StreamingIOEntry
    {
        interface IImpl
        {
            UniTask<bool> DirectoryExists();
            UniTask<bool> FileExists();

            IUniTaskAsyncEnumerable<string> GetDirectories();
            IUniTaskAsyncEnumerable<FilePath> GetAllDirectories();

            IUniTaskAsyncEnumerable<string> GetFiles();
            IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData();

            IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns);
            IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns);

            IUniTaskAsyncEnumerable<FilePath> GetAllFiles();
            IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData();

            IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns);
            IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns);

            UniTask<byte[]> ReadAllBytes();
            UniTask<string> ReadAllText();

            IUniTaskAsyncEnumerable<string> ReadLines();

            UniTask<Stream> OpenRead();

            UniTask<FileMetaData> GetFileMetaData();
        }
    }
}