#nullable enable
using RuniOS.IO;
using RuniOS.Resource;
using System.IO;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor
{
    public static partial class EditorResourcePack
    {
        public static ResourcePack? pack { get; private set; }

        public const string streamingAssetsFolderName = "EditorStreamingAssets";

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded()
        {
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Select(x => new PhysicalIOProvider((PhysicalPath)x.resolvedPath / streamingAssetsFolderName, SandboxPolicy.Disabled))
                .OfType<IIOProvider>();

            IIOProvider[] providers = [new PhysicalIOProvider((PhysicalPath)Directory.GetCurrentDirectory() / "Assets" / streamingAssetsFolderName, SandboxPolicy.Disabled), .. packages];
            GroupIOProvider provider = new GroupIOProvider(providers);

            pack = ResourcePack.Create("editor", provider, RequiredPackSort.BeforeVanilla);
        }

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => pack?.Dispose();
    }
}