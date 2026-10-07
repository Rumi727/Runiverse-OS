#nullable enable
namespace RuniOS.IO.Locations.Windows
{
    public sealed class WindowsMount : Mount
    {
        public string volumeName { get; }

        public override PhysicalPath mountPoint { get; }

        public override string? fileSystemType { get; }

        public override bool isReadOnly { get; }

        public override bool isSystemInternal => false;

        internal WindowsMount(string volumeName, PhysicalPath mountPoint, string? fileSystemType, bool isReadOnly)
        {
            this.volumeName = volumeName;
            this.mountPoint = mountPoint;
            this.fileSystemType = fileSystemType;
            this.isReadOnly = isReadOnly;
        }
    }
}