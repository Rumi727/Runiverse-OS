#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.Providers;
using RuniOS.Resource;
using System.IO;

namespace RuniOS.IO.Locations.Linux
{
    public sealed partial class LinuxMountProvider : IMountProvider
    {
        LinuxMountProvider() { }

        public static LinuxMountProvider instance { get; } = new LinuxMountProvider();

        public static readonly Identifier id = new Identifier("runios", "mount/linux");
        Identifier IProvider.id => id;

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