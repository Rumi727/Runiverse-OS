#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public class InstanceAssetHandle<TAsset>(TAsset assetObject) : IAssetHandle<TAsset>
    {
        /// <summary>
        /// 참조한 인스턴스를 가져옵니다.
        /// </summary>
        public TAsset assetObject { get; } = assetObject;
        bool IAssetHandle.isLoading => false;

        /// <inheritdoc cref="IAssetHandle.isSealed"/>
        public bool isSealed { get; private set; }
        bool IAssetHandle.isSealed { get => isSealed; set => isSealed = value; }
        
        UniTask<IAssetScope<TAsset>?> IAssetHandle<TAsset>.GetScope()
        {
            if (isSealed)
            {
                Debug.LogWarning("Cannot create a new AssetScope from sealed InstanceAssetHandle.");
                return UniTask.FromResult<IAssetScope<TAsset>?>(null);
            }
            
            return UniTask.FromResult<IAssetScope<TAsset>?>(new InstanceAssetScope<TAsset>(this, assetObject));
        }
        
        UniTask<IAssetScope?> IAssetHandle.GetScope()
        {
            if (isSealed)
            {
                Debug.LogWarning("Cannot create a new AssetScope from sealed InstanceAssetHandle.");
                return UniTask.FromResult<IAssetScope?>(null);
            }
            
            return UniTask.FromResult<IAssetScope?>(new InstanceAssetScope<TAsset>(this, assetObject));
        }

        public virtual bool IsSameTarget(IAssetHandle other)
        {
            if (other is not InstanceAssetHandle<TAsset> otherHandle)
                return false;

            return EqualityComparer<TAsset>.Default.Equals(assetObject, otherHandle.assetObject);
        }
    }
}
