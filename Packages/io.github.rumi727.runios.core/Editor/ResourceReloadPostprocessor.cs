#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    sealed class ResourceReloadPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            if (didDomainReload || importedAssets
                    .Concat(deletedAssets)
                    .Concat(movedAssets)
                    .Any(x => x.Contains("StreamingAssets", StringComparison.OrdinalIgnoreCase)))
                ResourceManager.Reload().Forget();
        }
    }
}
