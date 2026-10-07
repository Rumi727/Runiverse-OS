#nullable enable
using System.IO;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Represents a network filesystem mounted in the current process's mount namespace.<br/>
    /// 현재 프로세스의 마운트 네임스페이스에 마운트된 네트워크 파일 시스템을 나타냅니다.
    /// </summary>
    public sealed class LinuxNetworkLocation : NetworkLocation
    {
        readonly PhysicalIOProvider physicalIOProvider;

        /// <summary>
        /// Gets the Linux mount represented by this network location.<br/>
        /// 이 네트워크 위치가 나타내는 Linux 마운트를 가져옵니다.
        /// </summary>
        public LinuxMount linuxMount { get; }

        /// <summary>
        /// Gets the last mount point path segment, with the full mount point as fallback.<br/>
        /// 마운트 경로의 마지막 세그먼트를 가져오며, 없으면 전체 마운트 경로를 가져옵니다.
        /// </summary>
        public override string displayName { get; }

        /// <inheritdoc/>
        public override IIOProvider ioProvider => physicalIOProvider;

        /// <inheritdoc/>
        public override bool isReadOnly => linuxMount.isReadOnly;

        internal LinuxNetworkLocation(LinuxMount mount)
        {
            linuxMount = mount;

            displayName = GetDisplayName(mount.mountPoint);
            physicalIOProvider = new PhysicalIOProvider(mount.mountPoint);
        }

        /// <summary>
        /// Gets storage capacity and free space for the current mount point.<br/>
        /// 현재 마운트 경로의 전체 용량과 여유 공간을 가져옵니다.
        /// </summary>
        /// <param name="info">
        /// Receives the measured space values, or <see langword="default"/> when the query fails.<br/>
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

        static string GetDisplayName(PhysicalPath mountPoint)
        {
            ReadOnlySpan<char> mountPointSpan = [];
            foreach (var item in mountPoint.GetSegmentsSpan())
                mountPointSpan = item;

            return mountPointSpan.IsEmpty ? mountPoint.value : mountPointSpan.ToString();
        }
    }
}