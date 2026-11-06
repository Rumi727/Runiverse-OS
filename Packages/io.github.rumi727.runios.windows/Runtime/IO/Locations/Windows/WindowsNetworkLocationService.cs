#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Services;
using RuniOS.Resource;
using System.IO;

namespace RuniOS.IO.Locations.Windows
{
    /// <summary>
    /// Enumerates Windows mapped network drives.<br/>
    /// Windows에 매핑된 네트워크 드라이브를 열거합니다.
    /// </summary>
    public sealed class WindowsNetworkLocationService : INetworkLocationService
    {
        WindowsNetworkLocationService() { }

        /// <summary>
        /// Gets the Windows network location service instance.<br/>
        /// Windows 네트워크 위치 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static WindowsNetworkLocationService instance { get; } = new();

        /// <summary>
        /// The identifier of the Windows network location service.<br/>
        /// Windows 네트워크 위치 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "network-location/windows");
        Identifier IService.id => id;

        /// <summary>
        /// Enumerates logical drives whose type is <see cref="DriveType.Network"/>.<br/>
        /// 종류가 <see cref="DriveType.Network"/>인 논리 드라이브를 열거합니다.
        /// </summary>
        /// <returns>
        /// A sequence containing one network location for each mapped network drive.<br/>
        /// 매핑된 네트워크 드라이브마다 하나의 네트워크 위치를 포함하는 시퀀스를 반환합니다.
        /// </returns>
        /// <exception cref="IOException">
        /// May be thrown if the logical drives cannot be enumerated.<br/>
        /// 논리 드라이브를 열거할 수 없으면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// May be thrown if access to drive information is denied during enumeration.<br/>
        /// 열거 중 드라이브 정보에 대한 접근이 거부되면 발생할 수 있습니다.
        /// </exception>
        public static IEnumerable<WindowsNetworkLocation> EnumerateLocations()
        {
            foreach (DriveInfo driveInfo in DriveInfo.GetDrives())
            {
                if (driveInfo.DriveType == DriveType.Network)
                    yield return new WindowsNetworkLocation(driveInfo);
            }
        }

        IUniTaskAsyncEnumerable<NetworkLocation> INetworkLocationService.EnumerateLocations() => EnumerateLocations().ToUniTaskAsyncEnumerable();

#if UNITY_EDITOR_WIN || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
#endif
    }
}
