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
        /// Searches registries that provide the target asset type in descending <see cref="IAssetRegistry.priority"/> order and uses the first registry containing the asset identifier.<br/>
        /// 대상 에셋 타입을 제공하는 레지스트리를 <c>priority</c> 내림차순으로 탐색하고, 에셋 식별자를 포함한 첫 레지스트리를 사용합니다.
        /// </summary>
        automatic,

        /// <summary>
        /// Resolves the target through the exact registry and asset identifier in its <see cref="ResourceKey"/>. Registry priority is not used.<br/>
        /// <see cref="ResourceKey"/>의 레지스트리와 에셋 식별자로 정확한 레지스트리만 조회하며, 레지스트리 우선순위는 사용하지 않습니다.
        /// </summary>
        registry,

        /// <summary>
        /// Uses the asset instance stored in the reference directly.<br/>
        /// 참조에 저장된 에셋 인스턴스를 직접 사용합니다.
        /// </summary>
        direct
    }
}
