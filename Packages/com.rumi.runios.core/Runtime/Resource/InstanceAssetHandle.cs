#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public readonly record struct InstanceAssetHandle<TAsset>(TAsset assetObject, AssetImportData? importData = null) : IAssetHandle<TAsset> where TAsset : notnull
    {
        /// <inheritdoc/>
        public AssetImportData importData { get; } = importData ?? AssetImportData.empty;

        /// <summary>
        /// 참조한 인스턴스를 가져옵니다.
        /// </summary>
        public TAsset assetObject { get; } = assetObject;
        bool IAssetHandle.isLoading => false;

        bool IAssetHandle.isSealed => false;

        /// <summary>
        /// Creates a scope for the referenced instance without loading it from a resource pack.<br/>
        /// 리소스 팩에서 로드하지 않고 참조된 인스턴스의 스코프를 생성합니다.
        /// </summary>
        /// <returns>
        /// A scope containing the referenced asset instance.<br/>
        /// 참조된 에셋 인스턴스를 포함하는 스코프를 반환합니다.
        /// </returns>
        public InstanceAssetScope<TAsset>? GetScope() => new InstanceAssetScope<TAsset>(this, assetObject);

        UniTask<IAssetScope<TAsset>?> IAssetHandle<TAsset>.GetScope() => UniTask.FromResult<IAssetScope<TAsset>?>(new InstanceAssetScope<TAsset>(this, assetObject));

        UniTask<IAssetScope?> IAssetHandle.GetScope() => UniTask.FromResult<IAssetScope?>(new InstanceAssetScope<TAsset>(this, assetObject));

        public bool IsSameTarget(IAssetHandle other)
        {
            if (other is not InstanceAssetHandle<TAsset> otherHandle)
                return false;

            return EqualityComparer<TAsset>.Default.Equals(assetObject, otherHandle.assetObject) && importData.IsSameTarget(otherHandle.importData);
        }

        /// <inheritdoc/>
        void IAssetHandle.Seal() { }
    }
}
