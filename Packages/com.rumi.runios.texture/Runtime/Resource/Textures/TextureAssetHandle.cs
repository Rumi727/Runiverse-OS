#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Textures;

namespace RuniOS.Resource.Textures
{
    public sealed class TextureAssetHandle(IONode node, FileMetaData fileMetaData, AssetSidecar sidecar) : AssetHandle<Texture2D>(node, fileMetaData), IAssetSidecarHandle
    {
        public AssetSidecar sidecar { get; } = sidecar;

        protected override async UniTask<Texture2D?> Load()
        {
            await sidecar.Reload();

            TextureLoadSettings settings = sidecar.GetValue<TextureLoadSettings>(TextureAssetRegistry.id) ?? TextureLoadSettings.defaultValue;
            Texture2D texture = await TextureLoader.LoadAsync(node, settings);

            texture.name = node.path.GetFileNameWithoutExtension();
            texture.hideFlags = HideFlags.HideAndDontSave;

            return texture;
        }

        protected override void Unload(Texture2D unloadedAsset) => Object.DestroyImmediate(unloadedAsset);
    }
}