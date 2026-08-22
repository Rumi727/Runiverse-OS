#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Textures;

namespace RuniOS.Resource.Textures
{
    public sealed class TextureAssetHandle(IONode node, FileMetaData fileMetaData, AssetImportData importData) : AssetHandle<Texture2D>(node, fileMetaData, importData)
    {
        protected override async UniTask<Texture2D?> Load()
        {
            TextureLoadSettings settings = importData.GetValue<TextureLoadSettings>(TextureAssetRegistry.id);
            return await TextureLoader.LoadAsync(node, settings);
        }

        protected override void Unload(Texture2D unloadedAsset) => Object.DestroyImmediate(unloadedAsset);
    }
}