#nullable enable
#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using UnityEditor.PackageManager;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Modding
{
    // 0Harmony에 내장된 MonoMod의 CoreCLR JIT 훅은 해제되지 않아 자신이 로드된 ALC의 언로드를 막으므로,
    // 에디터에서는 0Harmony를 리로드되는 ALC 대신 Default ALC에 로드합니다.
    //
    // 이 로더는 0Harmony를 사용하는 코드와 같은 어셈블리(RuniOS.Modding)에 있어야 합니다.
    // 어셈블리 로드 순서는 보장되지 않으므로 에디터 어셈블리 등 다른 곳에 두면,
    // Harmony 형식을 처음 사용하는 시점에 AssemblyResolve 핸들러가 아직 등록되지 않았을 수 있습니다.
    static partial class HarmonyEditorLoader
    {
        const string harmonyAssemblyName = "0Harmony";
        const string harmonyPackagePath = "Packages/io.github.rumi727.nuget.lib.harmony";
        const string harmonyFileName = "0Harmony.dll";

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;

        static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
        {
            if (new AssemblyName(args.Name).Name != harmonyAssemblyName)
                return null;

            string packagePath = PackageInfo.FindForAssetPath(harmonyPackagePath).resolvedPath;
            return AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(packagePath, harmonyFileName));
        }
    }
}
#endif
