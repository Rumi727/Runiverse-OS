#nullable enable
using System.IO;

// ReSharper disable CommentTypo
// ReSharper disable IdentifierTypo
namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Represents a mounted Linux block device exposed by UDisks2.<br/>
    /// UDisks2가 제공하는 마운트된 Linux 블록 장치를 나타냅니다.
    /// </summary>
    public sealed class LinuxDeviceLocation : DeviceLocation
    {
        readonly PhysicalIOProvider physicalIOProvider;

        /// <summary>
        /// Gets the Linux mount matched to this device location.<br/>
        /// 이 장치 위치에 대응하는 Linux 마운트를 가져옵니다.
        /// </summary>
        public LinuxMount linuxMount { get; }

        /// <summary>
        /// Gets the UDisks2 block identifier.<br/>
        /// UDisks2 블록 식별자를 가져옵니다.
        /// </summary>
        public string blockId { get; }

        /// <summary>
        /// Gets the icon name reported by UDisks2.<br/>
        /// UDisks2가 보고한 아이콘 이름을 가져옵니다.
        /// </summary>
        public string? iconName { get; }

        /// <summary>
        /// Gets the symbolic icon name reported by UDisks2.<br/>
        /// UDisks2가 보고한 심볼릭 아이콘 이름을 가져옵니다.
        /// </summary>
        public string? symbolicIconName { get; }

        /// <summary>
        /// Gets the drive connection bus, or <see langword="null"/> when the block has no drive.<br/>
        /// 드라이브 연결 버스를 가져오며, 블록에 연결된 드라이브가 없으면 <see langword="null"/>을 가져옵니다.
        /// </summary>
        public string? connectionBus { get; }

        /// <inheritdoc/>
        public override Mount mount => linuxMount;

        /// <inheritdoc/>
        public override string? label { get; }

        /// <summary>
        /// Gets the preferred UDisks2 display name, with the mount point as fallback.<br/>
        /// UDisks2 표시 이름을 우선 사용하고, 없으면 마운트 경로를 표시 이름으로 가져옵니다.
        /// </summary>
        public override string displayName { get; }

        /// <inheritdoc/>
        public override IIOProvider ioProvider => physicalIOProvider;

        /// <inheritdoc/>
        public override bool isReadOnly => linuxMount.isReadOnly;

        /// <inheritdoc/>
        public override bool isRemovable { get; }

        /// <inheritdoc/>
        public override bool isOptical { get; }

        internal LinuxDeviceLocation
        (
            LinuxMount mount,
            string blockId,
            string? hintName,
            string? idLabel,
            string? driveVendor,
            string? driveModel,
            bool isRemovable,
            bool isOptical,
            string? iconName,
            string? symbolicIconName,
            string? connectionBus
        )
        {
            linuxMount = mount;
            this.blockId = blockId;
            this.isRemovable = isRemovable;
            this.isOptical = isOptical;
            this.symbolicIconName = !string.IsNullOrEmpty(symbolicIconName) ? symbolicIconName : null;
            this.iconName = !string.IsNullOrEmpty(iconName) ? iconName : null;
            this.connectionBus = connectionBus;

            label = idLabel;
            displayName = GetDisplayName(hintName, idLabel, driveVendor, driveModel, mount.mountPoint);
            physicalIOProvider = new PhysicalIOProvider(mount.mountPoint);
        }

        /// <summary>
        /// Gets storage capacity and free space for the current mount point.<br/>
        /// 현재 마운트 경로의 전체 용량과 여유 공간을 가져옵니다.
        /// </summary>
        /// <param name="info">
        /// Receives the measured space values, or the default value when the query fails.<br/>
        /// 조회한 공간 정보를 받으며, 조회에 실패하면 기본값을 받습니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the space query succeeds; otherwise, <see langword="false"/>.<br/>
        /// 공간 조회에 성공하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public override bool TryGetSpaceInfo(out StorageSpaceInfo info)
        {
            try
            {
                DriveInfo driveInfo = new(linuxMount.mountPoint.ToString());
                info = new StorageSpaceInfo
                (
                    (ulong)driveInfo.TotalSize,
                    (ulong)driveInfo.TotalFreeSpace,
                    (ulong)driveInfo.AvailableFreeSpace
                );

                return true;
            }
            catch (IOException)
            {
                info = default;
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                info = default;
                return false;
            }
        }

        static string GetDisplayName(string? hintName, string? idLabel, string? driveVendor, string? driveModel, PhysicalPath mountPoint)
        {
            string? name = GetNonWhiteSpaceValue(hintName) ?? GetNonWhiteSpaceValue(idLabel);
            if (name is not null)
                return name;

            string? vendor = GetNonWhiteSpaceValue(driveVendor);
            string? model = GetNonWhiteSpaceValue(driveModel);
            if (vendor is not null && model is not null)
                return $"{vendor} {model}";
            if (model is not null)
                return model;
            if (vendor is not null)
                return vendor;

            string path = mountPoint.ToString();
            string trimmedPath = path.TrimEnd(Path.DirectorySeparatorChar);
            string mountName = Path.GetFileName(trimmedPath);
            return string.IsNullOrWhiteSpace(mountName) ? path : mountName;
        }

        static string? GetNonWhiteSpaceValue(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}