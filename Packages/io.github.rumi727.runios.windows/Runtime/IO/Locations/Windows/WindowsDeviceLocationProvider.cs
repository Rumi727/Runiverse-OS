#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;

namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Enumerates local Windows logical drives that have a matching volume mount.<br/>
    /// 대응하는 볼륨 마운트가 있는 Windows 로컬 논리 드라이브를 열거합니다.
    /// </summary>
    public sealed class WindowsDeviceLocationProvider : IDeviceLocationProvider
    {
        WindowsDeviceLocationProvider() { }

        /// <summary>
        /// Gets the Windows device location provider instance.<br/>
        /// Windows 장치 위치 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static WindowsDeviceLocationProvider instance { get; } = new();

        /// <summary>
        /// Enumerates logical drives other than network drives whose roots match current Windows mount points.<br/>
        /// 네트워크 드라이브를 제외하고, 루트가 현재 Windows 마운트 경로와 일치하는 논리 드라이브를 열거합니다.
        /// </summary>
        /// <returns>
        /// A sequence containing one device location for each matching logical drive.<br/>
        /// 조건에 맞는 논리 드라이브마다 하나의 장치 위치를 포함하는 시퀀스를 반환합니다.
        /// </returns>
        /// <exception cref="IOException">
        /// May be thrown if the logical drives cannot be enumerated.<br/>
        /// 논리 드라이브를 열거할 수 없으면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// May be thrown if access to drive information is denied during enumeration.<br/>
        /// 열거 중 드라이브 정보에 대한 접근이 거부되면 발생할 수 있습니다.
        /// </exception>
        public static IEnumerable<WindowsDeviceLocation> EnumerateLocations()
        {
            Dictionary<PhysicalPath, WindowsMount> mountsByPoint = new();
            foreach (WindowsMount mount in WindowsMountProvider.EnumerateMounts())
                mountsByPoint[mount.mountPoint] = mount;

            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType == DriveType.Network)
                    continue;

                PhysicalPath rootPath = PhysicalPath.From(driveInfo.Name);
                if (!mountsByPoint.TryGetValue(rootPath, out WindowsMount? mount) || mount is null)
                    continue;

                yield return new WindowsDeviceLocation(mount, driveInfo);
            }
        }

        IUniTaskAsyncEnumerable<DeviceLocation> IDeviceLocationProvider.EnumerateLocations() => EnumerateLocations().ToUniTaskAsyncEnumerable();

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => IDeviceLocationProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IDeviceLocationProvider.Unregister(instance);
#endif
    }
}
