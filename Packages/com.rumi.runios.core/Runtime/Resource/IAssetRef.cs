#nullable enable
namespace RuniOS.Resource
{
    /// <summary>
    /// Exposes the common non-generic state and editor operations of an asset reference.<br/>
    /// 에셋 참조의 공통 비제네릭 상태와 에디터 작업을 노출합니다.
    /// </summary>
    public interface IAssetRef
    {
        /// <summary>
        /// Gets the asset type targeted by this reference.<br/>
        /// 이 참조가 대상으로 하는 에셋 타입을 가져옵니다.
        /// </summary>
        Type targetAssetType { get; }

        /// <summary>
        /// Gets the mode used to resolve the reference.<br/>
        /// 참조를 확인하는 데 사용하는 모드를 가져옵니다.
        /// </summary>
        AssetRefMode mode { get; }

        /// <summary>
        /// Gets the key associated with the reference. It is used when <see cref="mode"/> is <see cref="AssetRefMode.key"/>.<br/>
        /// 참조에 연결된 키를 가져옵니다. <see cref="mode"/>가 <see cref="AssetRefMode.key"/>일 때 사용됩니다.
        /// </summary>
        ResourceKey key { get; }

        /// <summary>
        /// Gets the direct asset instance associated with the reference, or <see langword="null"/> when none is available.<br/>
        /// 참조에 연결된 직접 에셋 인스턴스를 가져오며, 사용할 수 있는 인스턴스가 없으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        object? directAsset { get; }

        /// <summary>
        /// Returns a copy of the reference with the specified mode.<br/>
        /// 지정된 모드로 변경한 참조의 복사본을 반환합니다.
        /// </summary>
        /// <param name="mode">
        /// The mode to assign to the copy.<br/>
        /// 복사본에 설정할 모드입니다.
        /// </param>
        /// <returns>
        /// A copy with the specified mode.<br/>
        /// 지정된 모드가 설정된 복사본을 반환합니다.
        /// </returns>
        IAssetRef WithMode(AssetRefMode mode);

        /// <summary>
        /// Returns a copy of the reference with the specified resource key.<br/>
        /// 지정된 리소스 키로 변경한 참조의 복사본을 반환합니다.
        /// </summary>
        /// <param name="key">
        /// The resource key to assign to the copy.<br/>
        /// 복사본에 설정할 리소스 키입니다.
        /// </param>
        /// <returns>
        /// A copy with the specified resource key.<br/>
        /// 지정된 리소스 키가 설정된 복사본을 반환합니다.
        /// </returns>
        IAssetRef WithKey(ResourceKey key);

        /// <summary>
        /// Returns a copy of the reference with the specified direct asset instance.<br/>
        /// 지정된 직접 에셋 인스턴스로 변경한 참조의 복사본을 반환합니다.
        /// </summary>
        /// <param name="asset">
        /// The asset instance to assign to the copy.<br/>
        /// 복사본에 설정할 에셋 인스턴스입니다.
        /// </param>
        /// <returns>
        /// A copy with the specified direct asset instance.<br/>
        /// 지정된 직접 에셋 인스턴스가 설정된 복사본을 반환합니다.
        /// </returns>
        /// <exception cref="InvalidCastException">
        /// Thrown when <paramref name="asset"/> cannot be cast to <see cref="targetAssetType"/>.<br/>
        /// <paramref name="asset"/>을 <see cref="targetAssetType"/>으로 변환할 수 없을 때 발생합니다.
        /// </exception>
        IAssetRef WithDirect(object asset);
    }
}
