#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Sounds;
using RuniOS.IO;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource.Sounds
{
    public class WaveAudioAssetHandle(IONode node, FileMetaData metaData, AssetImportSettings<WaveAudioAssetImportSettings> importSettings) : AssetHandle<WaveAudioClip>(node, metaData)
    {
        public AssetImportSettings<WaveAudioAssetImportSettings> importSettings { get; } = importSettings;

        protected override async UniTask<WaveAudioClip?> Load()
        {
            await importSettings.Reload();

            WaveAudioAssetImportSettings data = importSettings.value;
            return data.loadMode switch
            {
                WaveAudioAssetLoadMode.normal => await SoundSystem.main.CreateSoundAsync(node),
                WaveAudioAssetLoadMode.keepCompressed => await SoundSystem.main.CreateSoundAsync(node, true),
                WaveAudioAssetLoadMode.stream => await SoundSystem.main.CreateStreamAsync(node),
                _ => null
            };
        }

        protected override void Unload(WaveAudioClip unloadedAsset) => unloadedAsset.Dispose();

        protected override bool IsDefaultAsset([NotNullWhen(false)] WaveAudioClip? asset) => asset == null || asset.isDisposed;

        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other) || other is not WaveAudioAssetHandle otherHandle)
                return false;

            if (assetObject == null || assetObject.isDisposed || !importSettings.IsSameTarget(otherHandle.importSettings))
                return false;

            return true;
        }
    }
}