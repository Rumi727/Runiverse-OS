#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.UIElements;

namespace RuniOS.Resource.VectorImages
{
    public sealed partial class VectorImageAssetRegistry : SimpleAssetRegistry<VectorImageAssetHandle>
    {
        /// <inheritdoc cref="registryId" />
        public static readonly Identifier id = new Identifier("runios", "vectors");

        /// <inheritdoc/>
        public override Identifier registryId => id;

        /// <inheritdoc/>
        public override Type providedAssetType => typeof(VectorImage);

        /// <inheritdoc/>
        public override IPatternMatcher assetMatcher { get; } = new ExtensionMatcher(".svg");

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<VectorImageAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<VectorImageAssetRegistry>();

        /// <inheritdoc/>
        protected override UniTask<VectorImageAssetHandle> CreateHandle(AssetDiscoveryContext context, Identifier identifier, IONode node, FileMetaData fileMetaData) => UniTask.FromResult(new VectorImageAssetHandle(node, fileMetaData));
    }
}
