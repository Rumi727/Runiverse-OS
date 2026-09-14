#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.Collections.Immutable;

namespace RuniOS.Resource.Sprites
{
    public sealed class SpriteAssetHandle(Identifier textureIdentifier, IONode node, FileMetaData fileMetaData, AssetSidecar sidecar) : AssetHandle<Sprite>(node, fileMetaData), IAssetSidecarHandle
    {
        public Identifier textureIdentifier { get; } = textureIdentifier;
        public AssetSidecar sidecar { get; } = sidecar;

        public IAssetScope<Texture2D>? textureScope { get; private set; }
        public ImmutableArray<IAssetScope<Texture2D>?> secondarySpriteTextureScopes { get; private set; } = [];

        protected override async UniTask<Sprite?> Load()
        {
            textureScope = await ResourceManager.LoadScopeAsync<Texture2D>(textureIdentifier);
            if (textureScope == null)
                return null;

            await sidecar.Reload();
            SpriteLoadSettings loadSettings = sidecar.GetValue<SpriteLoadSettings>(SpriteAssetRegistry.id) ?? SpriteLoadSettings.defaultValue;

            var scopeBuilder = ImmutableArray.CreateBuilder<IAssetScope<Texture2D>?>(loadSettings.secondaryTextures.Count);
            SecondarySpriteTexture[] secondarySpriteTextures = new SecondarySpriteTexture[loadSettings.secondaryTextures.Count];

            for (int i = 0; i < loadSettings.secondaryTextures.Count; i++)
            {
                var secondaryTexture = loadSettings.secondaryTextures[i];
                var scope = await ResourceManager.LoadScopeAsync<Texture2D>(secondaryTexture.identifier);

                scopeBuilder.Add(scope);
                secondarySpriteTextures[i] = new SecondarySpriteTexture
                {
                    name = secondaryTexture.name,
                    texture = scope?.asset ? scope.asset : Texture2D.missingTexture
                };
            }

            secondarySpriteTextureScopes = scopeBuilder.MoveToImmutable();

            Texture2D texture = textureScope.asset;
            Sprite sprite = Sprite.Create
            (
                texture,
                loadSettings.rect ?? new Rect(0, 0, texture.width, texture.height),
                loadSettings.pivot,
                loadSettings.pixelsPerUnit,
                loadSettings.extrude,
                loadSettings.meshType,
                loadSettings.border,
                loadSettings.generateFallbackPhysicsShape,
                secondarySpriteTextures
            );

            sprite.name = node.path.GetFileNameWithoutExtension();
            sprite.hideFlags = HideFlags.HideAndDontSave;

            return sprite;
        }

        protected override void Unload(Sprite unloadedAsset)
        {
            Object.DestroyImmediate(unloadedAsset);

            textureScope?.Dispose();
            foreach (var item in secondarySpriteTextureScopes)
                item?.Dispose();
        }
    }
}