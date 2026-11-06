#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Services;
using RuniOS.Resource;
using System.IO;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Enumerates network filesystems mounted in the current process's mount namespace.<br/>
    /// 현재 프로세스의 마운트 네임스페이스에 마운트된 네트워크 파일 시스템을 열거합니다.
    /// </summary>
    public sealed partial class LinuxNetworkLocationService : INetworkLocationService
    {
        LinuxNetworkLocationService() { }

        /// <summary>
        /// Gets the Linux network location service instance.<br/>
        /// Linux 네트워크 위치 서비스 인스턴스를 가져옵니다.
        /// </summary>
        public static LinuxNetworkLocationService instance { get; } = new();

        /// <summary>
        /// The identifier of the Linux network location service.<br/>
        /// Linux 네트워크 위치 서비스의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "network-location/linux");
        Identifier IService.id => id;

        /// <summary>
        /// Enumerates current Linux mounts classified as network filesystems.<br/>
        /// 네트워크 파일 시스템으로 분류되는 현재 Linux 마운트를 열거합니다.
        /// </summary>
        /// <returns>
        /// A sequence containing one network location for each matching mount.<br/>
        /// 조건에 맞는 마운트마다 하나의 네트워크 위치를 포함하는 시퀀스를 반환합니다.
        /// </returns>
        /// <exception cref="IOException">
        /// May be thrown if reading the mount information fails during enumeration.<br/>
        /// 열거 중 마운트 정보를 읽지 못하면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// May be thrown if access to the mount information is denied during enumeration.<br/>
        /// 열거 중 마운트 정보에 대한 접근이 거부되면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="FormatException">
        /// May be thrown if a mount information entry is malformed during enumeration.<br/>
        /// 열거 중 마운트 정보 항목의 형식이 잘못된 경우 발생할 수 있습니다.
        /// </exception>
        public static IEnumerable<LinuxNetworkLocation> EnumerateLocations() => LinuxMountService.EnumerateMounts()
            .Where(LinuxMountUtility.IsNetworkFileSystem)
            .Select(mount => new LinuxNetworkLocation(mount));

        IUniTaskAsyncEnumerable<NetworkLocation> INetworkLocationService.EnumerateLocations() => EnumerateLocations().ToUniTaskAsyncEnumerable();

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ServiceRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ServiceRegistry.Unregister(instance);
#endif
    }
}