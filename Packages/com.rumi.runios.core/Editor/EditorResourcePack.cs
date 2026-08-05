#nullable enable
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    [InitializeOnLoad]
    public static class EditorResourcePack
    {
        public static ResourcePack pack { get; }

        static EditorResourcePack()
        {
            var packages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Select(x => new PhysicalIOProvider((PhysicalPath)x.resolvedPath / streamingAssetsFolderName, SandboxPolicy.Disabled))
                .OfType<IIOProvider>();

            IIOProvider[] providers = [new PhysicalIOProvider(projectPath / "Assets" / streamingAssetsFolderName, SandboxPolicy.Disabled), .. packages];
            GroupIOProvider provider = new GroupIOProvider(providers);

            pack = ResourcePack.Create("editor", provider, RequiredPackSort.BeforeVanilla);
        }

        public const string streamingAssetsFolderName = "EditorStreamingAssets";
    }
}