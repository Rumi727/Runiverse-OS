#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RuniOS.Resource
{
    /// <summary>
    /// Represents a resource reference that resolves either a <see cref="ResourceKey"/> or a direct asset instance.<br/>
    /// <see cref="ResourceKey"/> 또는 직접 에셋 인스턴스를 통해 리소스를 확인하는 참조입니다.
    /// </summary>
    /// <typeparam name="TAsset">
    /// The asset type resolved by this reference, such as <see cref="Texture2D"/>.<br/>
    /// 이 참조가 확인하는 에셋 타입입니다. 예를 들어 <see cref="Texture2D"/>가 될 수 있습니다.
    /// </typeparam>
    // AssetRefSerializationSuppressor는 `RuniOS.Resource.AssetRef`1` 메타데이터와 field.Type.OriginalDefinition을 직접 비교합니다.
    [Serializable]
    public record struct AssetRef<TAsset> : IAssetRef where TAsset : notnull
    {
        /// <summary>
        /// Initializes a key-based reference from a registry identifier and an asset identifier.<br/>
        /// 레지스트리 식별자와 에셋 식별자로 키 기반 참조를 초기화합니다.
        /// </summary>
        /// <param name="registryId">
        /// The identifier of the registry that contains the asset.<br/>
        /// 에셋을 포함하는 레지스트리의 식별자입니다.
        /// </param>
        /// <param name="valueId">
        /// The identifier of the asset inside the registry.<br/>
        /// 레지스트리 안에서 에셋을 식별하는 값입니다.
        /// </param>
        public AssetRef(Identifier registryId, Identifier valueId) : this(new ResourceKey(registryId, valueId)) { }

        /// <summary>
        /// Initializes a key-based reference from a <see cref="ResourceKey"/>.<br/>
        /// <see cref="ResourceKey"/>로 키 기반 참조를 초기화합니다.
        /// </summary>
        /// <param name="key">
        /// The key used to resolve the asset.<br/>
        /// 에셋을 확인하는 데 사용할 키입니다.
        /// </param>
        public AssetRef(ResourceKey key)
        {
            this.key = key;
            mode = AssetRefMode.key;
        }

        /// <summary>
        /// Initializes a direct reference to an asset instance.<br/>
        /// 에셋 인스턴스를 직접 참조하는 참조를 초기화합니다.
        /// </summary>
        /// <param name="asset">
        /// The asset instance to reference directly.<br/>
        /// 직접 참조할 에셋 인스턴스입니다.
        /// </param>
        public AssetRef(TAsset asset)
        {
            directAsset = asset;
            mode = AssetRefMode.direct;
        }

        Type IAssetRef.targetAssetType => typeof(TAsset);

        /// <summary>
        /// Gets or sets whether this reference resolves a key or uses a direct asset instance.<br/>
        /// 이 참조가 키를 확인할지 직접 에셋 인스턴스를 사용할지 가져오거나 설정합니다.
        /// </summary>
        public AssetRefMode mode = AssetRefMode.key;
        AssetRefMode IAssetRef.mode => mode;

        /// <summary>
        /// Gets or sets the resource key used when <see cref="mode"/> is <see cref="AssetRefMode.key"/>.<br/>
        /// <see cref="mode"/>가 <see cref="AssetRefMode.key"/>일 때 사용할 리소스 키를 가져오거나 설정합니다.
        /// </summary>
        public ResourceKey key = new ResourceKey();
        ResourceKey IAssetRef.key => key;

        /// <summary>
        /// Gets or sets the asset instance used when <see cref="mode"/> is <see cref="AssetRefMode.direct"/>.<br/>
        /// <see cref="mode"/>가 <see cref="AssetRefMode.direct"/>일 때 사용할 에셋 인스턴스를 가져오거나 설정합니다.
        /// </summary>
        public TAsset? directAsset = default;
        object? IAssetRef.directAsset => directAsset;

        /// <summary>
        /// Determines whether the current reference points to the same target as the specified scope.<br/>
        /// 현재 참조가 지정된 스코프와 같은 대상을 가리키는지 확인합니다.
        /// </summary>
        /// <param name="other">
        /// The scope whose asset target is compared with this reference; may be <see langword="null"/> when no target is available.<br/>
        /// 이 참조와 에셋 대상을 비교할 스코프입니다. 대상이 없으면 <see langword="null"/>일 수 있습니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when both sides have no target or resolve the same target; otherwise, <see langword="false"/>.<br/>
        /// 양쪽 모두 대상이 없거나 같은 대상을 확인하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool IsSameTarget(IAssetScope<TAsset>? other)
        {
            IAssetHandle<TAsset>? newHandle = GetHandle();
            if (newHandle == null)
                return other == null;

            return other != null && other.handle.IsSameTarget(newHandle);
        }

        /// <summary>
        /// Gets a handle for the current key or direct asset target.<br/>
        /// 현재 키 또는 직접 에셋 대상에 대한 핸들을 가져옵니다.
        /// </summary>
        /// <returns>
        /// A handle for the resolved target, or <see langword="null"/> when the direct asset or key-based asset is unavailable.<br/>
        /// 확인된 대상의 핸들을 반환하며, 직접 에셋이나 키 기반 에셋을 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public IAssetHandle<TAsset>? GetHandle()
        {
            if (mode == AssetRefMode.direct)
                return directAsset != null ? new InstanceAssetHandle<TAsset>(directAsset) : null;
            else
                return ResourceManager.GetHandle<TAsset>(key);
        }

        /// <summary>
        /// Asynchronously loads the current target and creates a scope for it.<br/>
        /// 현재 대상을 비동기로 로드하고 해당 대상의 스코프를 생성합니다.
        /// </summary>
        /// <returns>
        /// When the asynchronous operation completes, returns a scope for the loaded target, or <see langword="null"/> when the target is unavailable.<br/>
        /// 비동기 작업이 완료되면 로드된 대상의 스코프를 반환하며, 대상을 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public async UniTask<IAssetScope<TAsset>?> LoadScopeAsync()
        {
            if (mode == AssetRefMode.direct) // 최대한 박싱을 피하기 위해 의도적으로 GetHandle 메소드를 사용하지 않았습니다.
                return directAsset != null ? new InstanceAssetHandle<TAsset>(directAsset).GetScope() : null;
            else
                return await ResourceManager.LoadScopeAsync<TAsset>(key);
        }

        IAssetRef IAssetRef.WithMode(AssetRefMode mode) => this with { mode = mode };
        IAssetRef IAssetRef.WithKey(ResourceKey key) => this with { key = key };
        IAssetRef IAssetRef.WithDirect(object asset) => this with { directAsset = (TAsset)asset };
    }
}
