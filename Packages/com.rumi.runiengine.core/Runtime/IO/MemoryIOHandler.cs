#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections.Concurrent;
using System.Text;
using RuniEngine.Spans;
using System.Linq;
using System.Text.RegularExpressions;

namespace RuniEngine.IO
{
    /// <summary>
    /// Thread-safe
    /// </summary>
    public sealed class MemoryIOHandler : IOHandler
    {
        class VirtualDirectory
        {
            public readonly ConcurrentDictionary<FilePath, VirtualDirectory> directories = new();
            public readonly ConcurrentDictionary<FilePath, VirtualFile> files = new();

            public VirtualDirectory? GetDirectory(FilePath path)
            {
                if (path.IsEmpty())
                    return this;

                VirtualDirectory directory = this;
                foreach (var directoryName in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
                {
                    if (!directory.directories.TryGetValue(new string(directoryName), out directory))
                        return null;
                }

                return directory;
            }

            public VirtualFile? GetFile(FilePath path)
            {
                FilePath parentPath = path.GetParentPath();
                string fileName = path.GetFileName();

                return GetDirectory(parentPath)?.files.GetValueOrDefault(fileName);
            }
        }

        class VirtualFile
        {
            readonly IOHandler? ioHandler = empty;
            readonly byte[] content = new byte[0];

            VirtualFile(IOHandler ioHandler) => this.ioHandler = ioHandler;
            VirtualFile(byte[] content) => this.content = content;


            public UniTask<byte[]> ReadAllBytesAsync() => ioHandler?.ReadAllBytes() ?? UniTask.RunOnThreadPool(() => content);

            public UniTask<string> ReadAllTextAsync() => ioHandler?.ReadAllText() ?? UniTask.RunOnThreadPool(() => Encoding.UTF8.GetString(content));

            public UniTask<IEnumerable<string>> ReadLines() => ioHandler?.ReadLines() ?? UniTask.RunOnThreadPool(() => Encoding.UTF8.GetString(content).ReadLines());

            public UniTask<Stream> OpenRead() => ioHandler?.OpenRead() ?? UniTask.RunOnThreadPool(() => (Stream)new MemoryStream(content, false));

            public static VirtualFile CreateShortcut(IOHandler ioHandler) => new VirtualFile(ioHandler);

            public static VirtualFile Create(byte[] content) => new VirtualFile(content.ToArray());
            public static VirtualFile Create(string content) => new VirtualFile(Encoding.UTF8.GetBytes(content));
        }

        public MemoryIOHandler() => rootDirectory = new VirtualDirectory();
        MemoryIOHandler(VirtualDirectory rootDirectory, MemoryIOHandler? parent, string childPath) : base(parent, childPath) => this.rootDirectory = rootDirectory;

        public new MemoryIOHandler? parent => (MemoryIOHandler?)base.parent;



        readonly VirtualDirectory rootDirectory;



        /// <returns><see cref="MemoryIOHandler"/><br/>유니티 qt이 공변 반환 타입 지원 안하네 tllllllllqkf</returns>
        public override IOHandler CreateChild(FilePath path)
        {
            MemoryIOHandler handler = this;
            if (string.IsNullOrEmpty(path))
                return handler;

            foreach (var item in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
            {
                string childPath = new string(item);
                handler = new MemoryIOHandler(rootDirectory, handler, childPath);
            }

            return handler;
        }

        public override IOHandler AddExtension(string extension) => new MemoryIOHandler(rootDirectory, parent, childPath + extension);



        public override UniTask<bool> DirectoryExists() => UniTask.FromResult(rootDirectory.GetDirectory(childFullPath) != null);

        public override UniTask<bool> FileExists() => UniTask.FromResult(rootDirectory.GetFile(childFullPath) != null);

        public override UniTask<IEnumerable<FilePath>> GetDirectories()
        {
            VirtualDirectory? directory = rootDirectory.GetDirectory(childFullPath) ?? throw new DirectoryNotFoundException(childFullPath);
            return UniTask.FromResult(directory.directories.Select(x => x.Key));
        }

        public override UniTask<IEnumerable<FilePath>> GetAllDirectories() => UniTask.FromResult(InternalGetAllDirectories().Select(x => x.Key));

        IEnumerable<KeyValuePair<FilePath, VirtualDirectory>> InternalGetAllDirectories()
        {
            return Recurse(this, rootDirectory.GetDirectory(childFullPath) ?? throw new DirectoryNotFoundException(childFullPath), FilePath.empty);

            static IEnumerable<KeyValuePair<FilePath, VirtualDirectory>> Recurse(MemoryIOHandler handler, VirtualDirectory directory, FilePath parentPath)
            {
                foreach (var item in directory.directories)
                {
                    FilePath path = parentPath + item.Key;
                    yield return new(path, item.Value);

                    foreach (var subItem in Recurse(handler, item.Value, path))
                        yield return subItem;
                }
            }
        }


        public override UniTask<IEnumerable<FilePath>> GetFiles()
        {
            VirtualDirectory? directory = rootDirectory.GetDirectory(childFullPath) ?? throw new DirectoryNotFoundException(childFullPath);
            return UniTask.FromResult(directory.files.Select(item => item.Key));
        }

        public override UniTask<IEnumerable<FilePath>> GetFiles(ExtensionFilter extensionFilter)
        {
            VirtualDirectory? directory = rootDirectory.GetDirectory(childFullPath) ?? throw new DirectoryNotFoundException(childFullPath);
            return UniTask.FromResult(FilterFiles(directory.files.Keys, extensionFilter));
        }

        public override UniTask<IEnumerable<FilePath>> GetAllFiles() => UniTask.FromResult(InternalGetAllFiles());

        IEnumerable<FilePath> InternalGetAllFiles()
        {
            var directories = InternalGetAllDirectories();
            return directories.SelectMany(directoryItem => directoryItem.Value.files.Select(fileItem => directoryItem.Key + fileItem.Key));
        }

        public override UniTask<IEnumerable<FilePath>> GetAllFiles(ExtensionFilter extensionFilter) => UniTask.FromResult(FilterFiles(InternalGetAllFiles(), extensionFilter));

        static IEnumerable<FilePath> FilterFiles(IEnumerable<FilePath> files, ExtensionFilter extensionFilter)
        {
            IEnumerable<string> patterns = extensionFilter.ToString().Split('|').Select(ConvertPatternToRegex);

            // `*` 패턴이 포함되어 있다면 바로 모든 파일 반환
            if (patterns.Contains(".*"))
                return files;

            return files.Where(file => patterns.Any(pattern => Regex.IsMatch(file, pattern, RegexOptions.IgnoreCase))).ToList();
        }

        static string ConvertPatternToRegex(string pattern)
        {
            if (pattern == "*" || pattern == "*.*")
                return ".*"; // 모든 파일을 허용하는 패턴

            string escaped = Regex.Escape(pattern).Replace(@"\*", ".*"); // '*'를 '.*'로 변환
            return $"^{escaped}$";
        }

        public override UniTask<byte[]> ReadAllBytes() => rootDirectory.GetFile(childFullPath)?.ReadAllBytesAsync() ?? throw new FileNotFoundException();

        public override UniTask<string> ReadAllText() => rootDirectory.GetFile(childFullPath)?.ReadAllTextAsync() ?? throw new FileNotFoundException();

        public override UniTask<IEnumerable<string>> ReadLines() => rootDirectory.GetFile(childFullPath)?.ReadLines() ?? throw new FileNotFoundException();

        public override UniTask<Stream> OpenRead() => rootDirectory.GetFile(childFullPath)?.OpenRead() ?? throw new FileNotFoundException();



        /*VirtualDirectory GetNearestDirectory(string path, out string childPath)
        {
            MemoryIOHandler? ioHandler = this;
            VirtualDirectory? directory = this.directory;
            childPath = string.Empty;

            while (directory == null)
            {
                string childPath = ioHandler.childPath;
                ioHandler = parent;

                if (ioHandler == null)
                    break;

                directory = ioHandler.directory;
                childPath = PathUtility.Combine(childPath, childPath);
            }

            return directory;
        }

        VirtualDirectory? GetDirectory(string path)
        {
            VirtualDirectory? directory = GetNearestDirectory(out string nearPath);
            return directory?.GetDirectory(PathUtility.Combine(nearPath, path));
        }*/
    }
}
