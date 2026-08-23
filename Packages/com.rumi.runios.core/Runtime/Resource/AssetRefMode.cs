#nullable enable
namespace RuniOS.Resource
{
    /// <summary>
    /// Specifies how an <see cref="IAssetRef"/> resolves its target.<br/>
    /// <see cref="IAssetRef"/>가 대상을 확인하는 방법을 지정합니다.
    /// </summary>
    public enum AssetRefMode
    {
        /// <summary>
        /// Resolves the target through its <see cref="ResourceKey"/>.<br/>
        /// <see cref="ResourceKey"/>를 통해 대상을 확인합니다.
        /// </summary>
        key,

        /// <summary>
        /// Uses the asset instance stored in the reference directly.<br/>
        /// 참조에 저장된 에셋 인스턴스를 직접 사용합니다.
        /// </summary>
        direct
    }
}
