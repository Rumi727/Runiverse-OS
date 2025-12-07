#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public class InstanceAssetHandle<TAsset> : IAssetHandle<TAsset>
    {
        public InstanceAssetHandle(TAsset assetObject)
        {
            this.assetObject = assetObject;
            scope = new InstanceAssetScope<TAsset>(this, assetObject);
        }

        /// <summary>
        /// 참조한 인스턴스를 가져옵니다.
        /// </summary>
        public TAsset assetObject { get; }
        bool IAssetHandle.isLoading => false;

        readonly InstanceAssetScope<TAsset> scope;
        
        UniTask<IAssetScope<TAsset>?> IAssetHandle<TAsset>.GetScope() => UniTask.FromResult<IAssetScope<TAsset>?>(scope);
        UniTask<IAssetScope?> IAssetHandle.GetScope() => UniTask.FromResult<IAssetScope?>(scope);
        
        public virtual bool IsSameTarget(IAssetHandle other)
        {
            if (other is not InstanceAssetHandle<TAsset> otherHandle)
                return false;

            return EqualityComparer<TAsset>.Default.Equals(assetObject, otherHandle.assetObject);
        }
    }
}