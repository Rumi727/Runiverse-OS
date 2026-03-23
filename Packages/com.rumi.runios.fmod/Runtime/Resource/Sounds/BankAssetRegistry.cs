#nullable enable
using Cysharp.Threading.Tasks;
using FMOD.Studio;
using RuniOS.Booting;
using RuniOS.IO;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Sounds
{
    public sealed class BankAssetRegistry : SimpleAssetRegistry<BankAssetHandle>
    {
        public override Identifier registryId => new Identifier("runios", "banks");
        public override string registryName => "banks";
        
        public override bool isDefault => true;

        public override Type assetType => typeof(Bank);
        
        public override WildcardPatterns assetFilter { get; } = "bank";
        
        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => AssetRegistryManager.Register<BankAssetRegistry>();

        protected override UniTask<BankAssetHandle> CreateHandle(IIOEntry entry, FileMetaData metaData) => UniTask.FromResult(new BankAssetHandle(entry, metaData));
    }
}