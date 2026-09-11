#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Sounds;
using RuniOS.IO;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource.Sounds
{
    public class WaveAudioAssetHandle(IONode node, FileMetaData fileMetaData, AssetSidecar sidecar) : AssetHandle<WaveAudioClip>(node, fileMetaData)
    {
        public AssetSidecar sidecar { get; } = sidecar;

        protected override async UniTask<WaveAudioClip?> Load()
        {
            WaveAudioAssetImportData data = sidecar.GetValue<WaveAudioAssetImportData>(WaveAudioAssetRegistry.id);
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
            if (!base.IsSameTarget(other))
                return false;

            return assetObject != null && !assetObject.isDisposed;
        }
    }
}