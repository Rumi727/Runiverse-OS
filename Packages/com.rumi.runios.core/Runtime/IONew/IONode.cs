#nullable enable
using RuniOS.IO;

namespace RuniOS.IONew
{
    public readonly partial record struct IONode(IIOProvider provider, FilePath path = default)
    {
        public bool isValid => _provider != null;

        public IIOProvider provider => _provider ?? throw new InvalidOperationException("Invalid node! (provider is null)");
        readonly IIOProvider? _provider = provider;

        public IONode rootNode => provider.rootNode;

        public FilePath path { get; } = path;
        public string name => path.GetFileName();

        public Directory dir => new Directory(this);
        public File file => new File(this);

        public IONode GetParent() => new IONode(provider, path.GetParentPath());

        public IONode CreateChild(FilePath childPath)
        {
            if (childPath.IsEmpty())
                return this;

            return new IONode(provider, path + childPath);
        }

        public IONode AddExtension(FileExtension extension) => new IONode(provider, path.value + extension);
    }
}