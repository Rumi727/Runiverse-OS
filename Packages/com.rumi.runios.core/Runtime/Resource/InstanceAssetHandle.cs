#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public class InstanceAssetHandle<TAsset>(TAsset assetObject) : IAssetHandle<TAsset> where TAsset : notnull
    {
        /// <summary>
        /// 참조한 인스턴스를 가져옵니다.
        /// </summary>
        public TAsset assetObject { get; } = assetObject;
        bool IAssetHandle.isLoading => false;

        bool IAssetHandle.isSealed => false;

        UniTask<IAssetScope<TAsset>?> IAssetHandle<TAsset>.GetScope() => UniTask.FromResult<IAssetScope<TAsset>?>(new InstanceAssetScope<TAsset>(this, assetObject));

        UniTask<IAssetScope?> IAssetHandle.GetScope() => UniTask.FromResult<IAssetScope?>(new InstanceAssetScope<TAsset>(this, assetObject));

        public virtual bool IsSameTarget(IAssetHandle other)
        {
            if (other is not InstanceAssetHandle<TAsset> otherHandle)
                return false;

            return EqualityComparer<TAsset>.Default.Equals(assetObject, otherHandle.assetObject);
        }

        /// <inheritdoc/>
        void IAssetHandle.Seal() { }
    }
}
