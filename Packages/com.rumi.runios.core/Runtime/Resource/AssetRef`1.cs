#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace RuniOS.Resource
{
    /// <summary>
    /// Represents a resource reference that resolves an asset automatically, through an exact <see cref="ResourceKey"/>, or through a direct asset instance.<br/>
    /// 자동 탐색, 정확한 <see cref="ResourceKey"/>, 또는 직접 에셋 인스턴스를 통해 에셋을 확인하는 참조입니다.
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
        /// Initializes an automatic reference from an asset identifier.<br/>
        /// 에셋 식별자로 자동 탐색 참조를 초기화합니다.
        /// </summary>
        /// <param name="assetId">
        /// The asset identifier to search for in compatible registries.<br/>
        /// 호환 레지스트리에서 검색할 에셋 식별자입니다.
        /// </param>
        public AssetRef(Identifier assetId)
        {
            this.assetId = assetId;
            mode = AssetRefMode.automatic;
        }

        /// <summary>
        /// Initializes a registry-mode reference from a registry identifier and an asset identifier.<br/>
        /// 레지스트리 식별자와 에셋 식별자로 레지스트리 모드 참조를 초기화합니다.
        /// </summary>
        /// <param name="registryId">
        /// The identifier of the registry that contains the asset.<br/>
        /// 에셋을 포함하는 레지스트리의 식별자입니다.
        /// </param>
        /// <param name="assetId">
        /// The identifier of the asset inside the registry.<br/>
        /// 레지스트리 안에서 에셋을 식별하는 값입니다.
        /// </param>
        public AssetRef(Identifier registryId, Identifier assetId) : this(new ResourceKey(registryId, assetId)) { }

        /// <summary>
        /// Initializes a registry-mode reference from a <see cref="ResourceKey"/>.<br/>
        /// <see cref="ResourceKey"/>로 레지스트리 모드 참조를 초기화합니다.
        /// </summary>
        /// <param name="resourceKey">
        /// The resource key used to resolve the asset.<br/>
        /// 에셋을 확인하는 데 사용할 리소스 키입니다.
        /// </param>
        public AssetRef(ResourceKey resourceKey)
        {
            this.resourceKey = resourceKey;
            mode = AssetRefMode.registry;
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
        /// Gets or sets how this reference resolves its target.<br/>
        /// 이 참조가 대상을 확인하는 방법을 가져오거나 설정합니다.
        /// </summary>
        public AssetRefMode mode = AssetRefMode.registry;
        AssetRefMode IAssetRef.mode => mode;

        /// <summary>
        /// Gets or sets the asset identifier used when <see cref="mode"/> is <see cref="AssetRefMode.automatic"/>.<br/>
        /// <see cref="mode"/>가 <see cref="AssetRefMode.automatic"/>일 때 사용할 에셋 식별자를 가져오거나 설정합니다.
        /// </summary>
        public Identifier assetId = Identifier.empty;
        Identifier IAssetRef.assetId => assetId;

        /// <summary>
        /// Gets or sets the resource key used when <see cref="mode"/> is <see cref="AssetRefMode.registry"/>.<br/>
        /// <see cref="mode"/>가 <see cref="AssetRefMode.registry"/>일 때 사용할 리소스 키를 가져오거나 설정합니다.
        /// </summary>
        public ResourceKey resourceKey = new ResourceKey();
        ResourceKey IAssetRef.resourceKey => resourceKey;

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
        /// Gets a handle for the current automatic, registry, or direct asset target.<br/>
        /// 현재 자동, 레지스트리, 또는 직접 에셋 대상에 대한 핸들을 가져옵니다.
        /// </summary>
        /// <returns>
        /// A handle for the resolved target, or <see langword="null"/> when the selected asset is unavailable.<br/>
        /// 확인된 대상의 핸들을 반환하며, 선택한 에셋을 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// <see cref="AssetRefMode.automatic"/> searches compatible registries by priority, <see cref="AssetRefMode.registry"/> uses only <see cref="resourceKey"/>, and <see cref="AssetRefMode.direct"/> uses <see cref="directAsset"/> without querying a registry.<br/>
        /// <see cref="AssetRefMode.automatic"/>은 호환 레지스트리를 우선순위에 따라 탐색하고, <see cref="AssetRefMode.registry"/>는 <see cref="resourceKey"/>만 사용하며, <see cref="AssetRefMode.direct"/>는 레지스트리를 조회하지 않고 <see cref="directAsset"/>을 사용합니다.
        /// </remarks>
        public IAssetHandle<TAsset>? GetHandle() => mode switch
        {
            AssetRefMode.automatic => ResourceManager.GetHandle<TAsset>(assetId),
            AssetRefMode.registry => ResourceManager.GetHandle<TAsset>(resourceKey),
            AssetRefMode.direct => directAsset != null ? new InstanceAssetHandle<TAsset>(directAsset) : null,
            _ => null
        };

        /// <summary>
        /// Asynchronously loads the current target and creates a scope for it.<br/>
        /// 현재 대상을 비동기로 로드하고 해당 대상의 스코프를 생성합니다.
        /// </summary>
        /// <returns>
        /// When the asynchronous operation completes, returns a scope for the loaded target, or <see langword="null"/> when the target is unavailable.<br/>
        /// 비동기 작업이 완료되면 로드된 대상의 스코프를 반환하며, 대상을 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// Resolution follows the same mode-specific rules as <see cref="GetHandle()"/>.<br/>
        /// 확인 규칙은 <see cref="GetHandle()"/>과 동일하게 모드별로 적용됩니다.
        /// </remarks>
        public async UniTask<IAssetScope<TAsset>?> LoadScopeAsync() => mode switch
        {
            // 최대한 박싱을 피하기 위해 의도적으로 GetHandle 메소드를 사용하지 않았습니다.
            AssetRefMode.direct => directAsset != null ? new InstanceAssetHandle<TAsset>(directAsset).GetScope() : null,
            AssetRefMode.automatic => await ResourceManager.LoadScopeAsync<TAsset>(assetId),
            AssetRefMode.registry => await ResourceManager.LoadScopeAsync<TAsset>(resourceKey),
            _ => null
        };

        IAssetRef IAssetRef.WithMode(AssetRefMode mode) => this with { mode = mode };
        IAssetRef IAssetRef.WithAssetId(Identifier assetId) => this with { assetId = assetId };
        IAssetRef IAssetRef.WithResourceKey(ResourceKey resourceKey) => this with { resourceKey = resourceKey };
        IAssetRef IAssetRef.WithDirect(object? asset) => this with { directAsset = asset is null ? default : (TAsset)asset };
    }
}
