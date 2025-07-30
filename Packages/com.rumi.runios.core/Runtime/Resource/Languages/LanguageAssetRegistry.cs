#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace RuniOS.Resource.Languages
{
    public class LanguageAssetRegistry : AssetRegistry
    {
        public override string registryName => "lang";

        public override bool isLoading => _isLoading;
        bool _isLoading;

        public override async UniTask Reload(IEnumerable<ResourcePackReference> resourcePacks)
        {
            _isLoading = true;
            
            foreach (var resourcePack in resourcePacks)
            {
                resourcePack.assetFolder.Recreate();
            }
            
            _isLoading = false;
        }
    }
}
