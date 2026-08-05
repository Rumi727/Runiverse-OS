#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Sounds;
using RuniOS.IO;

namespace RuniOS.Resource.Sounds
{
    public class WaveAudioAssetHandle(IONode node, FileMetaData metaData) : AssetHandle<WaveAudioClip>(node, metaData)
    {
        protected override async UniTask<WaveAudioClip?> Load() => await SoundSystem.main.CreateSoundAsync(node);

        protected override void Unload() => assetObject?.Dispose();

        protected override bool IsDefaultAsset(WaveAudioClip? asset) => asset == null || asset.isDisposed;

        public override bool IsSameTarget(IAssetHandle other)
        {
            if (!base.IsSameTarget(other))
                return false;

            if (assetObject == null || assetObject.isDisposed)
                return false;

            return true;
        }
    }
}