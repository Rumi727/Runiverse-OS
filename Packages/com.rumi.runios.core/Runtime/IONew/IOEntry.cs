#nullable enable
using RuniOS.IO;

namespace RuniOS.IONew
{
    public readonly record struct IOEntry(FilePath path, IOMetaData metaData, bool isDirectory)
    {
        public FilePath path { get; } = path;
        public IOMetaData metaData { get; } = metaData;

        public bool isDirectory { get; } = isDirectory;
    }
}