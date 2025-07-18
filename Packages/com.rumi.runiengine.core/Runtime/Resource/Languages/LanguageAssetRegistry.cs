#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace RuniEngine.Resource
{
    public class LanguageAssetRegistry : AssetRegistry
    {
        public override string registryName => "lang";

        public override async UniTask Reload(IEnumerable<ResourcePackRef> resourcePacks) => await UniTask.CompletedTask;
    }
}
