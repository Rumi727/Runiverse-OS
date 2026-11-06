#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Providers;
using RuniOS.Resource;
using System.IO;

namespace RuniOS.IO.Locations.Linux
{
    /// <summary>
    /// Enumerates the mounts listed in <c>/proc/self/mountinfo</c>.<br/>
    /// <c>/proc/self/mountinfo</c>에 나열된 마운트를 열거합니다.
    /// </summary>
    public sealed partial class LinuxMountProvider : IMountProvider
    {
        LinuxMountProvider() { }

        /// <summary>
        /// Gets the Linux mount provider instance.<br/>
        /// Linux 마운트 프로바이더 인스턴스를 가져옵니다.
        /// </summary>
        public static LinuxMountProvider instance { get; } = new LinuxMountProvider();

        /// <summary>
        /// The identifier of the Linux mount provider.<br/>
        /// Linux 마운트 프로바이더의 식별자입니다.
        /// </summary>
        public static readonly Identifier id = new Identifier("runios", "mount/linux");
        Identifier IProvider.id => id;

        /// <summary>
        /// Enumerates the mounts listed in <c>/proc/self/mountinfo</c>.<br/>
        /// <c>/proc/self/mountinfo</c>에 나열된 마운트를 열거합니다.
        /// </summary>
        /// <returns>
        /// A sequence containing one mount for each entry of the mount information.<br/>
        /// 마운트 정보의 항목마다 하나의 마운트를 포함하는 시퀀스를 반환합니다.
        /// </returns>
        /// <exception cref="UnauthorizedAccessException">
        /// May be thrown if access to the mount information is denied during enumeration.<br/>
        /// 열거 중 마운트 정보에 대한 접근이 거부되면 발생할 수 있습니다.
        /// </exception>
        /// <exception cref="FormatException">
        /// May be thrown if a mount information entry is malformed during enumeration.<br/>
        /// 열거 중 마운트 정보 항목의 형식이 잘못된 경우 발생할 수 있습니다.
        /// </exception>
        public static IEnumerable<LinuxMount> EnumerateMounts()
        {
            using StreamReader reader = new("/proc/self/mountinfo");
            while (reader.ReadLine() is {  } line)
                yield return LinuxMountParser.Parse(line);
        }

        IUniTaskAsyncEnumerable<Mount> IMountProvider.EnumerateMounts() => EnumerateMounts().ToUniTaskAsyncEnumerable();

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => ProviderRegistry.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => ProviderRegistry.Unregister(instance);
#endif
    }
}