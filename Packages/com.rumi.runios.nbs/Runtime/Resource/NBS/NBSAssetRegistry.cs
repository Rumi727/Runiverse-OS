#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.NBS;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.NBS
{
    /// <summary>
    /// Registers files below <c>assets/&lt;namespace&gt;/nbs</c> as scoped <see cref="NBSFile"/> resources.<br/>
    /// <c>assets/&lt;namespace&gt;/nbs</c> 아래 파일을 스코프 기반 <see cref="NBSFile"/> 리소스로 등록합니다.
    /// </summary>
    public sealed partial class NBSAssetRegistry : SimpleAssetRegistry<NBSAssetHandle>
    {
        /// <summary>Gets the active registry instance.<br/>활성 레지스트리 인스턴스를 가져옵니다.</summary>
        public static NBSAssetRegistry instance => AssetRegistryManager.Get<NBSAssetRegistry>() ?? new NBSAssetRegistry();

        /// <inheritdoc/>
        public override Identifier registryId => new Identifier("runios", "nbs");

        /// <inheritdoc/>
        public override RuniPath registryName => RuniPath.From("nbs");

        /// <inheritdoc/>
        public override bool isDefault => true;

        /// <inheritdoc/>
        public override Type assetType => typeof(NBSFile);

        /// <inheritdoc/>
        public override WildcardPatterns assetFilter => WildcardPatterns.nbsFileFilter;

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<NBSAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<NBSAssetRegistry>();

        protected override UniTask<NBSAssetHandle> CreateHandle(IONode node, FileMetaData metaData) => UniTask.FromResult(new NBSAssetHandle(node, metaData));
    }
}
