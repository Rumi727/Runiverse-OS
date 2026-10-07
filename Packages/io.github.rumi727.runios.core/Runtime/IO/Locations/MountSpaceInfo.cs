#nullable enable
namespace RuniOS.IO.Locations
{
    public readonly record struct MountSpaceInfo(ulong totalSize, ulong freeSize, ulong availableSize);
}