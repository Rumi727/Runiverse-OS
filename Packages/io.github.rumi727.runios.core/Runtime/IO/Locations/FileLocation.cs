#nullable enable
namespace RuniOS.IO.Locations
{
    public abstract class FileLocation
    {
        public abstract string displayName { get; }

        public abstract IIOProvider ioProvider { get; }

        public abstract bool isReadOnly { get; }

        public abstract bool TryGetSpaceInfo(out StorageSpaceInfo info);
    }
}