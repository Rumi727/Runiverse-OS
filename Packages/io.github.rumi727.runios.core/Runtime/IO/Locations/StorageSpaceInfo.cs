#nullable enable
namespace RuniOS.IO.Locations
{
    public readonly record struct StorageSpaceInfo(ulong totalSize, ulong freeSize, ulong availableSize);
}