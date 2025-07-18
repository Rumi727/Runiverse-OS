#nullable enable
using Cysharp.Threading.Tasks;
using RuniEngine.Spans;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RuniEngine.IO
{
    public class FileIOHandler : IOHandler
    {
        public FileIOHandler(FilePath path) : base() => realPath = path;
        FileIOHandler(FileIOHandler? parent, FilePath childPath) : base(parent, childPath) => realPath = parent?.realPath + childPath;

        public new FileIOHandler? parent => (FileIOHandler?)base.parent;



        public FilePath realPath { get; } = string.Empty;



        /// <returns><see cref="FileIOHandler"/><br/>유니티 qt이 공변 반환 타입 지원 안하네 tllllllllqkf</returns>
        public override IOHandler CreateChild(FilePath path)
        {
            FileIOHandler handler = this;
            if (path.IsEmpty())
                return handler;

            foreach (var item in path.value.AsSpan().SplitAny(FilePath.directorySeparatorChars))
            {
                FilePath childPath = FilePath.Create(item);
                handler = new FileIOHandler(handler, childPath);
            }
            
            return handler;
        }

        public override IOHandler AddExtension(string extension) => new FileIOHandler(parent, childPath.AddExtension(extension));



        public override UniTask<bool> DirectoryExists() => UniTask.RunOnThreadPool(() => Directory.Exists(realPath));

        public override UniTask<bool> FileExists() => UniTask.RunOnThreadPool(() => File.Exists(realPath));

        public override UniTask<IEnumerable<FilePath>> GetDirectories() => UniTask.RunOnThreadPool(() => Directory.EnumerateDirectories(realPath).Select(x => realPath - x));

        public override UniTask<IEnumerable<FilePath>> GetAllDirectories() => UniTask.RunOnThreadPool(() => Directory.EnumerateDirectories(realPath, "*", SearchOption.AllDirectories).Select(x => realPath - x));

        public override UniTask<IEnumerable<FilePath>> GetFiles() => UniTask.RunOnThreadPool(() => Directory.EnumerateFiles(realPath).Select(x => realPath - x));
        public override UniTask<IEnumerable<FilePath>> GetFiles(ExtensionFilter extensionFilter) => UniTask.RunOnThreadPool(() => DirectoryUtility.EnumerateFiles(realPath, extensionFilter).Select(x => realPath - x));

        public override UniTask<IEnumerable<FilePath>> GetAllFiles() => UniTask.RunOnThreadPool(() => Directory.EnumerateFiles(realPath, "*", SearchOption.AllDirectories).Select(x => realPath - x));
        public override UniTask<IEnumerable<FilePath>> GetAllFiles(ExtensionFilter extensionFilter) => UniTask.RunOnThreadPool(() => DirectoryUtility.EnumerateFiles(realPath, extensionFilter, SearchOption.AllDirectories).Select(x => realPath - x));

        public override UniTask<byte[]> ReadAllBytes() => File.ReadAllBytesAsync(realPath).AsUniTask();

        public override UniTask<string> ReadAllText() => File.ReadAllTextAsync(realPath).AsUniTask();

        public override UniTask<IEnumerable<string>> ReadLines() => UniTask.RunOnThreadPool(() => File.ReadLines(realPath));

        /// <returns><see cref="FileStream"/><br/>유니티 qt이 공변 반환 타입 지원 안하네 tllllllllqkf</returns>
        public override UniTask<Stream> OpenRead() => UniTask.RunOnThreadPool(() => (Stream)File.OpenRead(realPath));
    }
}
