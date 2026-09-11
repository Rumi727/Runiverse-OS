#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Resource.Textures;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.Sprites
{
    public sealed partial class SpriteAssetRegistry : SimpleAssetRegistry<SpriteAssetHandle>
    {
        /// <inheritdoc cref="registryId" />
        public static readonly Identifier id = new Identifier("runios", "sprites");

        /// <inheritdoc/>
        public override Identifier registryId => id;

        /// <inheritdoc/>
        public override RuniPath registryName => TextureAssetRegistry.id.path;

        /// <inheritdoc/>
        public override Type providedAssetType => typeof(Sprite);

        /// <inheritdoc/>
        public override IPatternMatcher assetMatcher => IPatternMatcher.pictureMatcher;

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<SpriteAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<SpriteAssetRegistry>();

        /// <inheritdoc/>
        protected override UniTask<SpriteAssetHandle> CreateHandle(Identifier identifier, IONode node, FileMetaData fileMetaData) => UniTask.FromResult(new SpriteAssetHandle(identifier, node, fileMetaData, CreateSidecar(node)));
    }
}