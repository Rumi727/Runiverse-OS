#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public interface IAssetHandle<TAsset> : IAssetHandle where TAsset : notnull
    {
        /// <summary>
        /// 로드된 실제 에셋 객체를 가져오거나 설정합니다.
        /// <br/>에셋이 언로드되었거나 아직 로드되지 않은 경우 <see langword="null"/>입니다.
        /// </summary>
        new TAsset? assetObject { get; }
        object? IAssetHandle.assetObject => assetObject;

        new UniTask<IAssetScope<TAsset>?> GetScope();
        async UniTask<IAssetScope?> IAssetHandle.GetScope() => await GetScope();
    }
}