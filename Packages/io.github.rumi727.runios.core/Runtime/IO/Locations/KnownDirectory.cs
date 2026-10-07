#nullable enable
namespace RuniOS.IO.Locations
{
    public readonly record struct KnownDirectory(KnownDirectoryType type, PhysicalPath path);
}