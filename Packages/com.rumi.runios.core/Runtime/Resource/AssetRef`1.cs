using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    /// <summary>
    /// 인스펙터에서 특정 타입의 리소스를 지정하기 위한 래퍼입니다.
    /// 실제 저장되는 데이터는 AssetKey 뿐입니다.
    /// </summary>
    /// <typeparam name="TAsset">목표 리소스 타입 (예: Texture2D, AudioClip)</typeparam>
    [Serializable]
    public record struct AssetRef<TAsset>(ResourceKey key) : IAssetRef
    {
        public AssetRef(Identifier registryId, Identifier valueId) : this(new ResourceKey(registryId, valueId)) { }

        [SerializeField] public ResourceKey key = key;

        Type IAssetRef.targetAssetType => typeof(TAsset);
        ResourceKey IAssetRef.key { get => key; set => key = value; }

        public readonly void Deconstruct(out Identifier registryId, out Identifier assetId) => key.Deconstruct(out registryId, out assetId);

        public async UniTask<IAssetScope<TAsset>?> LoadAsync() => await ResourceManager.LoadScopeAsync<TAsset>(key);
    }
}