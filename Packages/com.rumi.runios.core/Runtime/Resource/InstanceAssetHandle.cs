#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public sealed class InstanceAssetHandle<TAsset> : IAssetHandle<TAsset>
    {
        public InstanceAssetHandle(TAsset assetObject) => this.assetObject = assetObject;
        
        public TAsset assetObject { get; }
        public bool isLoading => false;
        
        public UniTask<IAssetScope<TAsset>?> GetScope() => UniTask.FromResult<IAssetScope<TAsset>?>(new InstanceAssetScope<TAsset>(this, assetObject));
        UniTask<IAssetScope?> IAssetHandle.GetScope() => UniTask.FromResult<IAssetScope?>(new InstanceAssetScope<TAsset>(this, assetObject));
        
        public bool IsSameTarget(IAssetHandle other)
        {
            if (other is not InstanceAssetHandle<TAsset> otherHandle)
                return false;

            return EqualityComparer<TAsset>.Default.Equals(assetObject, otherHandle.assetObject);
        }
    }
}