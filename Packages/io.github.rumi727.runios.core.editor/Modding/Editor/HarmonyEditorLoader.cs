#nullable enable
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor.Modding
{
    // 0Harmony에 내장된 MonoMod의 CoreCLR JIT 훅은 해제되지 않아 자신이 로드된 ALC의 언로드를 막으므로,
    // 에디터에서는 0Harmony를 리로드되는 ALC 대신 Default ALC에 로드합니다.
    static partial class HarmonyEditorLoader
    {
        const string harmonyAssemblyName = "0Harmony";
        const string harmonyPath = "Packages/io.github.rumi727.nuget.lib.harmony/0Harmony.dll";

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

        static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (new AssemblyName(args.Name).Name != harmonyAssemblyName)
                return null;

            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(harmonyPath));
        }
    }
}
