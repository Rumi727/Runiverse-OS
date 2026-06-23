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
    public record struct AssetRef<TAsset>(ResourceKey _key) : IAssetRef
    {
        public AssetRef(Identifier registryId, Identifier valueId) : this(new ResourceKey(registryId, valueId)) { }

        Type IAssetRef.targetAssetType => typeof(TAsset);
        public ResourceKey key { get => _key; set => _key = value; }

        [SerializeField] ResourceKey _key = _key;

        public readonly void Deconstruct(out Identifier registryId, out Identifier assetId) => _key.Deconstruct(out registryId, out assetId);

        public async UniTask<IAssetScope<TAsset>?> LoadAsync() => await ResourceManager.LoadScopeAsync<TAsset>(_key);
    }
}