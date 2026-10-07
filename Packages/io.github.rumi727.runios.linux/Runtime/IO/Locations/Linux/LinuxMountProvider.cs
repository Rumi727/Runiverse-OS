#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using System.IO;

namespace RuniOS.IO.Locations.Linux
{
    public sealed partial class LinuxMountProvider : IMountProvider
    {
        LinuxMountProvider() { }

        public static LinuxMountProvider instance { get; } = new LinuxMountProvider();

        public static IEnumerable<LinuxMount> EnumerateMounts()
        {
            using StreamReader reader = new("/proc/self/mountinfo");
            while (reader.ReadLine() is {  } line)
                yield return LinuxMountParser.Parse(line);
        }

        IUniTaskAsyncEnumerable<Mount> IMountProvider.EnumerateMounts() => EnumerateMounts().ToUniTaskAsyncEnumerable();

#if UNITY_EDITOR_LINUX || !UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnAssemblyLoaded]
        static void OnAssemblyLoaded() => IMountProvider.Register(instance);

        [Unity.Scripting.LifecycleManagement.OnAssemblyUnloading]
        static void OnAssemblyUnloading() => IMountProvider.Unregister(instance);
#endif
    }
}