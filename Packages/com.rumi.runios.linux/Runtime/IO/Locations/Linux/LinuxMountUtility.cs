#nullable enable
// ReSharper disable StringLiteralTypo
namespace RuniOS.IO.Locations.Linux
{
    static class LinuxMountUtility
    {
        static readonly HashSet<string> systemInternalFileSystems = new(StringComparer.Ordinal)
        {
            // Kernel / process interfaces
            "proc",
            "sysfs",

            // Device infrastructure
            "devtmpfs",
            "devpts",

            // Control / configuration interfaces
            "cgroup",
            "cgroup2",
            "configfs",

            // Kernel debugging / tracing
            "debugfs",
            "tracefs",

            // Security / firmware / kernel state
            "securityfs",
            "efivarfs",
            "pstore",
            "bpf",

            // Kernel IPC / special interfaces
            "mqueue",
            "binfmt_misc",

            // FUSE infrastructure itself
            "fusectl",

            // Namespace representation
            "nsfs",
        };

        public static bool IsSystemInternal(LinuxMount mount) => systemInternalFileSystems.Contains(mount.fileSystemType);

        public static bool IsNetworkFileSystem(LinuxMount mount) =>
            mount.fileSystemType is "nfs" or "nfs4" or "smbfs" or "cifs" or "smb3" or "fuse.sshfs" or "fuse.rclone" ||
            mount.source.StartsWith("//", StringComparison.Ordinal);
    }
}