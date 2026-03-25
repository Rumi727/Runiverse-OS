using Cysharp.Threading.Tasks;
using System.IO;

namespace RuniOS.IO
{
    public partial class StreamingIOEntry
    {
        class IOImpl : IImpl
        {
            public IOImpl(FilePath targetPath) => handler = new FileIOHandler(targetPath);

            readonly FileIOHandler handler;
            
            public UniTask<bool> DirectoryExists() => handler.DirectoryExists();
            public UniTask<bool> FileExists() => handler.FileExists();
            
            public IUniTaskAsyncEnumerable<string> GetDirectories() => handler.GetDirectories();
            public IUniTaskAsyncEnumerable<FilePath> GetAllDirectories() => handler.GetAllDirectories();
            
            public IUniTaskAsyncEnumerable<string> GetFiles() => handler.GetFiles();
            public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData() => handler.GetFilesWithMetaData();
            
            public IUniTaskAsyncEnumerable<string> GetFiles(WildcardPatterns wildcardPatterns) => handler.GetFiles(wildcardPatterns);
            public IUniTaskAsyncEnumerable<FileMetaData> GetFilesWithMetaData(WildcardPatterns wildcardPatterns) => handler.GetFilesWithMetaData(wildcardPatterns);
            
            public IUniTaskAsyncEnumerable<FilePath> GetAllFiles() => handler.GetAllFiles();
            public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData() => handler.GetAllFilesWithMetaData();
            
            public IUniTaskAsyncEnumerable<FilePath> GetAllFiles(WildcardPatterns wildcardPatterns) => handler.GetAllFiles(wildcardPatterns);
            public IUniTaskAsyncEnumerable<(FilePath relativePath, FileMetaData metaData)> GetAllFilesWithMetaData(WildcardPatterns wildcardPatterns) => handler.GetAllFilesWithMetaData(wildcardPatterns);
            
            public UniTask<byte[]> ReadAllBytes() => handler.ReadAllBytes();
            public UniTask<string> ReadAllText() => handler.ReadAllText();
            public IUniTaskAsyncEnumerable<string> ReadLines() => handler.ReadLines();
            
            public UniTask<Stream> OpenRead() => handler.OpenRead();
            
            public UniTask<FileMetaData> GetFileMetaData() => handler.GetFileMetaData();
        }
    }
}