#nullable enable
namespace RuniOS.Resource
{
    public interface IAssetScope<TAsset> : IAssetScope
    {
        /// <summary>
        /// 이 스코프가 참조하는 <see cref="AssetHandle{T}"/>을 가져옵니다.
        /// </summary>
        new IAssetHandle<TAsset> handle { get; }
        IAssetHandle IAssetScope.handle => handle;
        
        new TAsset asset { get; }
        object? IAssetScope.asset => asset;
    }
}