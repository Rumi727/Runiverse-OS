#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.Sounds;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.Sounds
{
    public sealed partial class WaveAudioAssetRegistry : SimpleAssetRegistry<WaveAudioAssetHandle>
    {
        public override Identifier registryId => new Identifier("runios", "waves");
        public override RuniPath registryName => RuniPath.From("sounds");

        public override bool isDefault => true;

        public override Type assetType => typeof(WaveAudioClip);

        public override IPatternMatcher assetMatcher => IPatternMatcher.musicMatcher;

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<WaveAudioAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<WaveAudioAssetRegistry>();

        protected override UniTask<WaveAudioAssetHandle> CreateHandle(IONode node, FileMetaData fileMetaData, AssetImportData importData) => UniTask.FromResult(new WaveAudioAssetHandle(node, fileMetaData, importData));
    }
}