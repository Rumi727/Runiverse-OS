#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Booting;
using RuniOS.IO;
using RuniOS.Sounds;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Sounds
{
    public sealed class WaveAudioAssetRegistry : SimpleAssetRegistry<WaveAudioAssetHandle>
    {
        public override Identifier registryId => new Identifier("runios", "waves");
        public override RuniPath registryName => RuniPath.From("sounds");

        public override bool isDefault => true;

        public override Type assetType => typeof(WaveAudioClip);

        public override WildcardPatterns assetFilter => WildcardPatterns.musicFileFilter;

        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => AssetRegistryManager.Register<WaveAudioAssetRegistry>();

        protected override UniTask<WaveAudioAssetHandle> CreateHandle(IONode node, FileMetaData metaData) => UniTask.FromResult(new WaveAudioAssetHandle(node, metaData));
    }
}