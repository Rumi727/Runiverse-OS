#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace RuniEngine.Resource
{
    public abstract class AssetRegistry
    {
        public abstract string registryName { get; }

        public Dictionary<Identifier, AssetHandle> assetHandles { get; } = new();

        public abstract UniTask Reload(IEnumerable<ResourcePackRef> resourcePacks);
    }
}
