#nullable enable
using RuniOS.Spans;
using System.Collections.Immutable;

namespace RuniOS.IO.Locations.Linux
{
    public sealed class LinuxMount : Mount
    {
        public int mountId { get; }
        public int parentMountId { get; }

        public uint majorDeviceNumber { get; }
        public uint minorDeviceNumber { get; }

        /// <summary>
        /// Root of the mount within the filesystem.
        /// </summary>
        public string root { get; }

        /// <summary>
        /// Mount point in the current process's mount namespace.
        /// </summary>
        public override PhysicalPath mountPoint { get; }

        /// <summary>
        /// Raw mount-specific options field.
        /// </summary>
        public string mountOptions { get; }

        /// <summary>
        /// Raw optional fields between mountOptions and "-".
        /// </summary>
        public ImmutableArray<string> optionalFields { get; }

        public override string fileSystemType { get; }

        public string source { get; }

        /// <summary>
        /// Raw superblock options field.
        /// </summary>
        public string superOptions { get; }

        public override bool isReadOnly { get; }

        public override bool isSystemInternal => LinuxMountUtility.IsSystemInternal(this);

        internal LinuxMount
        (
            int mountId,
            int parentMountId,
            uint majorDeviceNumber,
            uint minorDeviceNumber,
            string root,
            PhysicalPath mountPoint,
            string mountOptions,
            ImmutableArray<string> optionalFields,
            string fileSystemType,
            string source,
            string superOptions
        )
        {
            this.mountId = mountId;
            this.parentMountId = parentMountId;

            this.majorDeviceNumber = majorDeviceNumber;
            this.minorDeviceNumber = minorDeviceNumber;

            this.root = root;
            this.mountPoint = mountPoint;

            this.mountOptions = mountOptions;
            this.optionalFields = optionalFields;

            this.fileSystemType = fileSystemType;
            this.source = source;

            this.superOptions = superOptions;

            isReadOnly = HasOption(mountOptions, "ro");
        }

        static bool HasOption(string options, ReadOnlySpan<char> target)
        {
            foreach (ReadOnlySpan<char> item in options.AsSpan().Split(','))
            {
                if (item.SequenceEqual(target))
                    return true;
            }

            return false;
        }
    }
}