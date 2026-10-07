#nullable enable
using System.IO;
using System.Security;

namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Represents a local Windows logical drive with a matching volume mount.<br/>
    /// 대응하는 볼륨 마운트가 있는 Windows 로컬 논리 드라이브를 나타냅니다.
    /// </summary>
    public sealed class WindowsDeviceLocation : DeviceLocation
    {
        readonly PhysicalIOProvider physicalIOProvider;

        /// <summary>
        /// Gets the Windows mount matched to this device location.<br/>
        /// 이 장치 위치에 대응하는 Windows 마운트를 가져옵니다.
        /// </summary>
        public WindowsMount windowsMount { get; }

        /// <summary>
        /// Gets the logical drive represented by this device location.<br/>
        /// 이 장치 위치가 나타내는 논리 드라이브를 가져옵니다.
        /// </summary>
        public DriveInfo driveInfo { get; }

        /// <inheritdoc/>
        public override Mount mount => windowsMount;

        /// <inheritdoc/>
        public override string? label { get; }

        /// <summary>
        /// Gets the volume label followed by the drive name, with the drive name alone as fallback.<br/>
        /// 볼륨 라벨 뒤에 드라이브 이름을 붙인 표시 이름을 가져오며, 라벨이 없거나 조회할 수 없으면 드라이브 이름만 가져옵니다.
        /// </summary>
        public override string displayName { get; }

        /// <inheritdoc/>
        public override IIOProvider ioProvider => physicalIOProvider;

        /// <inheritdoc/>
        public override bool isReadOnly => windowsMount.isReadOnly;

        /// <summary>
        /// Gets whether the logical drive type is <see cref="DriveType.Removable"/>.<br/>
        /// 논리 드라이브 종류가 <see cref="DriveType.Removable"/>인지 여부를 가져옵니다.
        /// </summary>
        public override bool isRemovable { get; }

        /// <summary>
        /// Gets whether the logical drive type is <see cref="DriveType.CDRom"/>.<br/>
        /// 논리 드라이브 종류가 <see cref="DriveType.CDRom"/>인지 여부를 가져옵니다.
        /// </summary>
        public override bool isOptical { get; }

        internal WindowsDeviceLocation(WindowsMount mount, DriveInfo driveInfo)
        {
            windowsMount = mount;
            this.driveInfo = driveInfo;

            DriveType driveType = driveInfo.DriveType;
            isRemovable = driveType == DriveType.Removable;
            isOptical = driveType == DriveType.CDRom;

            try
            {
                if (driveInfo.IsReady)
                {
                    string volumeLabel = driveInfo.VolumeLabel;
                    label = string.IsNullOrWhiteSpace(volumeLabel) ? null : volumeLabel;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            displayName = GetDisplayName(driveInfo);
            physicalIOProvider = new PhysicalIOProvider(mount.mountPoint);
        }

        /// <summary>
        /// Gets storage capacity and free space for the logical drive.<br/>
        /// 논리 드라이브의 전체 용량과 여유 공간을 가져옵니다.
        /// </summary>
        /// <param name="info">
        /// Receives the measured space values, or <see langword="default"/> when the drive is not ready or accessible.<br/>
        /// 조회한 공간 정보를 받으며, 드라이브가 준비되지 않았거나 접근할 수 없으면 기본값을 받습니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the space query succeeds; otherwise, <see langword="false"/>.<br/>
        /// 공간 조회에 성공하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public override bool TryGetSpaceInfo(out StorageSpaceInfo info)
        {
            info = default;
            try
            {
                if (!driveInfo.IsReady)
                    return false;

                info = new StorageSpaceInfo
                (
                    (ulong)driveInfo.TotalSize,
                    (ulong)driveInfo.TotalFreeSpace,
                    (ulong)driveInfo.AvailableFreeSpace
                );

                return true;
            }
            catch (IOException) { return false; }
            catch (UnauthorizedAccessException) { return false; }
            catch (SecurityException) { return false; }
        }

        static string GetDisplayName(DriveInfo driveInfo)
        {
            string driveName = driveInfo.Name.TrimEnd(Path.DirectorySeparatorChar);
            try
            {
                if (driveInfo.IsReady)
                {
                    string volumeLabel = driveInfo.VolumeLabel;
                    if (!string.IsNullOrWhiteSpace(volumeLabel))
                        return $"{volumeLabel} ({driveName})";
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (SecurityException) { }

            return driveName;
        }
    }
}
