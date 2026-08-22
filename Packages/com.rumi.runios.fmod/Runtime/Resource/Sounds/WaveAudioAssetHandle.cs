#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Sounds;
using RuniOS.IO;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource.Sounds
{
    public class WaveAudioAssetHandle(IONode node, FileMetaData fileMetaData, AssetImportData importData) : AssetHandle<WaveAudioClip>(node, fileMetaData, importData)
    {
        protected override async UniTask<WaveAudioClip?> Load()
        {
            WaveAudioAssetImportData data = importData.GetValue<WaveAudioAssetImportData>(WaveAudioAssetRegistry.id);
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
            if (!base.IsSameTarget(other) || other is not WaveAudioAssetHandle)
                return false;

            if (assetObject == null || assetObject.isDisposed)
                return false;

            return true;
        }
    }
}