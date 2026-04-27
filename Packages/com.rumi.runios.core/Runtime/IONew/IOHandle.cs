#nullable enable
using RuniOS.IO;

namespace RuniOS.IONew
{
    public readonly partial record struct IOHandle(IWritableIOProvider provider, FilePath path = default)
    {
        public bool isValid => _provider != null;

        public IWritableIOProvider provider => _provider ?? throw new InvalidOperationException("Invalid Handle! (provider is null)");
        readonly IWritableIOProvider? _provider = provider;

        public IOHandle rootHandle => provider.rootNode;

        public FilePath path { get; } = path;
        public string name => path.GetFileName();

        public Directory dir => new Directory(this);
        public File file => new File(this);

        public IOHandle GetParent() => new IOHandle(provider, path.GetParentPath());

        public IOHandle CreateChild(FilePath childPath)
        {
            if (childPath.IsEmpty())
                return this;

            return new IOHandle(provider, path + childPath);
        }

        public IOHandle AddExtension(FileExtension extension) => new IOHandle(provider, path.value + extension);

        public static implicit operator IONode(IOHandle node) => new IONode(node.provider, node.path);
    }
}