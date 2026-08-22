#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.Textures
{
    public sealed partial class TextureAssetRegistry : SimpleAssetRegistry<TextureAssetHandle>
    {
        /// <inheritdoc cref="registryId" />
        public static readonly Identifier id = new Identifier("runios", "textures");

        /// <inheritdoc/>
        public override Identifier registryId => id;

        /// <inheritdoc/>
        public override Type assetType => typeof(Texture2D);

        /// <inheritdoc/>
        public override IPatternMatcher assetMatcher => IPatternMatcher.pictureMatcher;

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<TextureAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<TextureAssetRegistry>();

        /// <inheritdoc/>
        protected override UniTask<TextureAssetHandle> CreateHandle(IONode node, FileMetaData fileMetaData, AssetImportData importData) => UniTask.FromResult(new TextureAssetHandle(node, fileMetaData, importData));
    }
}