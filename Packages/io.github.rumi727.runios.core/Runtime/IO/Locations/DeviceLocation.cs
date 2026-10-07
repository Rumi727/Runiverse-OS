#nullable enable
namespace RuniOS.IO.Locations
{
    public abstract class DeviceLocation : FileLocation
    {
        public abstract Mount mount { get; }

        public abstract string? label { get; }

        public abstract bool isRemovable { get; }
        public abstract bool isOptical { get; }
    }
}