#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO.Locations.Linux.DBus;
using System.IO;
using System.Text;
using Tmds.DBus.Protocol;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Enumerates UDisks2 block devices that have a matching current Linux mount.<br/>
    /// 현재 Linux 마운트와 대응하는 UDisks2 블록 장치를 열거합니다.
    /// </summary>
    public sealed partial class LinuxDeviceLocationProvider : IDeviceLocationProvider
    {
        const string uDisks2ServiceName = "org.freedesktop.UDisks2";
        const string uDisks2ObjectManagerPath = "/org/freedesktop/UDisks2";

        LinuxDeviceLocationProvider() { }

        /// <summary>
        /// Gets the Linux device location provider instance.<br/>
        /// Linux 장치 위치 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static LinuxDeviceLocationProvider instance { get; } = new();

        /// <summary>
        /// Asynchronously enumerates mounted UDisks2 block devices.<br/>
        /// 마운트된 UDisks2 블록 장치를 비동기로 열거합니다.
        /// </summary>
        /// <returns>
        /// An asynchronous sequence containing one location for each mounted UDisks2 filesystem.<br/>
        /// 마운트된 UDisks2 파일 시스템마다 하나의 장치 위치를 포함하는 비동기 시퀀스를 반환합니다.
        /// </returns>
        public static IUniTaskAsyncEnumerable<DeviceLocation> EnumerateLocations() => UniTaskAsyncEnumerable.Create<DeviceLocation>(async (writer, iterationToken) =>
        {
            DBusConnection connection = DBusConnection.System;
            await connection.ConnectAsync();

            ObjectManager objectManager = new(connection, uDisks2ServiceName, new ObjectPath(uDisks2ObjectManagerPath));
            Dictionary<ObjectPath, Dictionary<string, Dictionary<string, VariantValue>>> objects = await objectManager.GetManagedObjectsAsync();

            Dictionary<PhysicalPath, LinuxMount> mountsByPoint = new();
            foreach (LinuxMount mount in LinuxMountProvider.EnumerateMounts())
                mountsByPoint[mount.mountPoint] = mount;

            Dictionary<ObjectPath, INullableDriveProperties> drivePropertiesCache = new();
            foreach ((ObjectPath objectPath, Dictionary<string, Dictionary<string, VariantValue>> interfaces) in objects)
            {
                iterationToken.ThrowIfCancellationRequested();

                if (!interfaces.ContainsKey(Filesystem.DBusInterfaceName) || !interfaces.ContainsKey(Block.DBusInterfaceName))
                    continue;

                Block block = new(connection, uDisks2ServiceName, objectPath);

                INullableBlockProperties blockProperties;
                try
                {
                    blockProperties = await block.GetNullablePropertiesAsync();
                }
                catch (DBusErrorReplyException e) when (IsObjectGone(e))
                {
                    continue;
                }

                if (blockProperties.HintIgnore ?? false)
                    continue;

                if (blockProperties.Id == null)
                    continue;

                Filesystem filesystem = new(connection, uDisks2ServiceName, objectPath);

                INullableFilesystemProperties filesystemProperties;
                try
                {
                    filesystemProperties = await filesystem.GetNullablePropertiesAsync();
                }
                catch (DBusErrorReplyException e) when (IsObjectGone(e))
                {
                    continue;
                }

                byte[][]? mountPoints = filesystemProperties.MountPoints;
                if (mountPoints == null)
                    continue;

                LinuxMount? selectedMount = null;
                foreach (byte[] mountPointBytes in mountPoints)
                {
                    int pathLength = mountPointBytes.Length;
                    while (pathLength > 0 && mountPointBytes[pathLength - 1] == 0)
                        pathLength--;

                    if (pathLength == 0)
                        continue;

                    string mountPath = Encoding.UTF8.GetString(mountPointBytes, 0, pathLength);
                    if (!Path.IsPathRooted(mountPath))
                        continue;

                    PhysicalPath physicalPath = PhysicalPath.From(mountPath);
                    if (!mountsByPoint.TryGetValue(physicalPath, out LinuxMount? linuxMount) || linuxMount == null)
                        continue;

                    if (selectedMount == null || IsBetterRepresentativeMount(linuxMount, selectedMount))
                        selectedMount = linuxMount;
                }

                if (selectedMount == null)
                    continue;

                INullableDriveProperties? driveProperties = null;
                ObjectPath? drivePath = blockProperties.Drive;

                if (drivePath != null && drivePath.Value.ToString() != "/")
                {
                    if (drivePropertiesCache.TryGetValue(drivePath.Value, out INullableDriveProperties? cachedDriveProperties))
                        driveProperties = cachedDriveProperties;
                    else
                    {
                        Drive drive = new(connection, uDisks2ServiceName, drivePath.Value);

                        try
                        {
                            driveProperties = await drive.GetNullablePropertiesAsync();
                            drivePropertiesCache.Add(drivePath.Value, driveProperties);
                        }
                        catch (DBusErrorReplyException e) when (IsObjectGone(e))
                        {

                        }
                    }
                }

                await writer.YieldAsync(new LinuxDeviceLocation
                (
                    selectedMount,
                    blockProperties.Id,
                    blockProperties.HintName,
                    blockProperties.IdLabel,
                    driveProperties?.Vendor,
                    driveProperties?.Model,
                    driveProperties?.Removable ?? false,
                    driveProperties?.Optical ?? false,
                    blockProperties.HintIconName,
                    blockProperties.HintSymbolicIconName,
                    driveProperties?.ConnectionBus
                ));
            }
        });

        static bool IsObjectGone(DBusErrorReplyException exception) =>
            exception.ErrorName is "org.freedesktop.DBus.Error.UnknownObject" or "org.freedesktop.DBus.Error.UnknownInterface";

        static bool IsBetterRepresentativeMount(LinuxMount candidate, LinuxMount current)
        {
            var candidateEnumerator = candidate.mountPoint.GetSegmentsSpan().GetEnumerator();
            var currentEnumerator = current.mountPoint.GetSegmentsSpan().GetEnumerator();

            while (true)
            {
                bool candidateHasNext = candidateEnumerator.MoveNext();
                bool currentHasNext = currentEnumerator.MoveNext();

                if (candidateHasNext != currentHasNext)
                    return !candidateHasNext;

                if (!candidateHasNext)
                    return false;
            }
        }

        IUniTaskAsyncEnumerable<DeviceLocation> IDeviceLocationProvider.EnumerateLocations() => EnumerateLocations();

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => IDeviceLocationProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IDeviceLocationProvider.Unregister(instance);
#endif
    }
}