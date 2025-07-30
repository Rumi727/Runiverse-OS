#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace RuniOS.Resource
{
    public abstract class AssetRegistry
    {
        public abstract string registryName { get; }

        public Dictionary<Identifier, AssetHandle> assetHandles { get; } = new();
        
        public abstract bool isLoading { get; }

        public abstract UniTask Reload(IEnumerable<ResourcePackReference> resourcePacks);
    }
}
