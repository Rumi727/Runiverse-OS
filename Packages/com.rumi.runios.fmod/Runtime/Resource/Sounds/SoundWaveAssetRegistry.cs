#nullable enable
using Cysharp.Threading.Tasks;
using FMOD;
using RuniOS.Booting;
using RuniOS.IO;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Sounds
{
    public sealed class SoundWaveAssetRegistry : SimpleAssetRegistry<SoundWaveAssetHandle>
    {
        public override Identifier registryId => new Identifier("runios", "waves");
        public override string registryName => "sounds";
        
        public override bool isDefault => true;

        public override Type assetType => typeof(Sound);

        public override WildcardPatterns assetFilter => WildcardPatterns.musicFileFilter;
        
        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => AssetRegistryManager.Register<SoundWaveAssetRegistry>();

        protected override UniTask<SoundWaveAssetHandle> CreateHandle(IIOEntry entry, FileMetaData metaData) => UniTask.FromResult(new SoundWaveAssetHandle(entry, metaData));
    }
}