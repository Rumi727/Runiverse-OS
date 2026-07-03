using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RuniOS.Resource
{
    /// <summary>
    /// 인스펙터에서 특정 타입의 리소스를 지정하기 위한 래퍼입니다.
    /// 실제 저장되는 데이터는 <see cref="ResourceKey"/> 뿐입니다.
    /// </summary>
    /// <typeparam name="TAsset">목표 리소스 타입 (예: <see cref="Texture2D"/>, <see cref="AudioClip"/>)</typeparam>
    [Serializable]
    public record struct AssetRef<TAsset>(ResourceKey key) : IAssetRef
    {
        public AssetRef(Identifier registryId, Identifier valueId) : this(new ResourceKey(registryId, valueId)) { }

        [SerializeField] public ResourceKey key = key;

        Type IAssetRef.targetAssetType => typeof(TAsset);
        ResourceKey IAssetRef.key => key;

        public bool IsSameTarget(IAssetScope<TAsset>? scope)
        {
            IAssetHandle<TAsset>? newHandle = ResourceManager.GetHandle<TAsset>(key);
            if (newHandle == null)
                return scope == null;

            return scope != null && scope.handle.IsSameTarget(newHandle);

        }

        public IAssetHandle<TAsset>? GetHandle() => ResourceManager.GetHandle<TAsset>(key);

        public async UniTask<IAssetScope<TAsset>?> LoadScopeAsync() => await ResourceManager.LoadScopeAsync<TAsset>(key);

        public readonly void Deconstruct(out Identifier registryId, out Identifier assetId) => key.Deconstruct(out registryId, out assetId);

        IAssetRef IAssetRef.WithKey(ResourceKey key) => new AssetRef<TAsset>(key);
    }
}