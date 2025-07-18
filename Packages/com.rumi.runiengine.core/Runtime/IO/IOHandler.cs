#nullable enable
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RuniEngine.IO
{
    public abstract class IOHandler
    {
        public static readonly IOHandler empty = new EmptyIOHandler();



        protected IOHandler() { }
        protected IOHandler(IOHandler? parent, FilePath childPath)
        {
            this.parent = parent;

            this.childPath = childPath;
            childFullPath = parent?.childFullPath + this.childPath;
        }



        public IOHandler? parent { get; }

        public FilePath childPath { get; } = new FilePath();
        public FilePath childFullPath { get; } = new FilePath();



        public abstract IOHandler CreateChild(FilePath path);
        public IOHandler CreateChild(FilePath path1, FilePath path2) => CreateChild(path1).CreateChild(path2);
        public IOHandler CreateChild(FilePath path1, FilePath path2, FilePath path3) => CreateChild(path1).CreateChild(path2).CreateChild(path3);
        public IOHandler CreateChild(FilePath path1, FilePath path2, FilePath path3, FilePath path4) => CreateChild(path1).CreateChild(path2).CreateChild(path3).CreateChild(path4);
        public IOHandler CreateChild(FilePath path1, FilePath path2, FilePath path3, FilePath path4, FilePath path5) => CreateChild(path1).CreateChild(path2).CreateChild(path3).CreateChild(path4).CreateChild(path5);
        public IOHandler CreateChild(params FilePath[] paths)
        {
            IOHandler handler = this;
            for (int i = 0; i < paths.Length; i++)
                handler = handler.CreateChild(paths[i]);

            return handler;
        }

        public abstract IOHandler AddExtension(string extension);

        public abstract UniTask<bool> DirectoryExists();

        public abstract UniTask<bool> FileExists();
        public async UniTask<IOHandler?> FileExists(ExtensionFilter extensionFilter)
        {
            for (int i = 0; i < extensionFilter.extensions.Count; i++)
            {
                string extension = extensionFilter.extensions[i];

                IOHandler extensionHandler = AddExtension(extension);
                if (await extensionHandler.FileExists())
                    return extensionHandler;
            }

            return null;
        }

        public abstract UniTask<IEnumerable<FilePath>> GetDirectories();

        public abstract UniTask<IEnumerable<FilePath>> GetAllDirectories();

        public abstract UniTask<IEnumerable<FilePath>> GetFiles();
        public abstract UniTask<IEnumerable<FilePath>> GetFiles(ExtensionFilter extensionFilter);

        public abstract UniTask<IEnumerable<FilePath>> GetAllFiles();
        public abstract UniTask<IEnumerable<FilePath>> GetAllFiles(ExtensionFilter extensionFilter);

        public abstract UniTask<byte[]> ReadAllBytes();

        public abstract UniTask<string> ReadAllText();

        public abstract UniTask<IEnumerable<string>> ReadLines();

        public abstract UniTask<Stream> OpenRead();

        sealed class EmptyIOHandler : IOHandler
        {
            public EmptyIOHandler() { }
            EmptyIOHandler(IOHandler? parent, string childPath) : base(parent, childPath) { }

            public override IOHandler CreateChild(FilePath path) => new EmptyIOHandler(this, path);
            public override IOHandler AddExtension(string extension) => new EmptyIOHandler(parent, childPath + extension);

            public override UniTask<bool> DirectoryExists() => UniTask.FromResult(false);

            public override UniTask<bool> FileExists() => UniTask.FromResult(false);

            public override UniTask<IEnumerable<FilePath>> GetDirectories() => UniTask.FromResult(Enumerable.Empty<FilePath>());

            public override UniTask<IEnumerable<FilePath>> GetAllDirectories() => UniTask.FromResult(Enumerable.Empty<FilePath>());

            public override UniTask<IEnumerable<FilePath>> GetFiles() => UniTask.FromResult(Enumerable.Empty<FilePath>());
            public override UniTask<IEnumerable<FilePath>> GetFiles(ExtensionFilter extensionFilter) => UniTask.FromResult(Enumerable.Empty<FilePath>());

            public override UniTask<IEnumerable<FilePath>> GetAllFiles() => UniTask.FromResult(Enumerable.Empty<FilePath>());
            public override UniTask<IEnumerable<FilePath>> GetAllFiles(ExtensionFilter extensionFilter) => UniTask.FromResult(Enumerable.Empty<FilePath>());

            public override UniTask<byte[]> ReadAllBytes() => new UniTask<byte[]>(Array.Empty<byte>());

            public override UniTask<string> ReadAllText() => new UniTask<string>(string.Empty);

            public override UniTask<IEnumerable<string>> ReadLines() => UniTask.FromResult(Enumerable.Empty<string>());

            public override UniTask<Stream> OpenRead() => UniTask.FromResult(Stream.Null);
        }
    }
}
