#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.Booting;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Localizations;
using System.Collections.ObjectModel;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Languages
{
    sealed class LanguageAssetRegistry : SimpleAssetRegistry<LanguageAssetHandle>
    {
        public override Identifier registryId => new Identifier("runios", "lang");
        public override string registryName => "lang";
        
        public override bool isDefault => true;

        public override Type assetType => typeof(LocalizationData);

        public override WildcardPatterns assetFilter => WildcardPatterns.jsonFileFilter;

        internal readonly Dictionary<string, Dictionary<Identifier, string>> calculatedAsset = new();
        
        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => AssetRegistryManager.Register<LanguageAssetRegistry>();

        protected override async UniTask<LanguageAssetHandle> CreateHandle(IIOEntry entry, FileMetaData metaData)
        {
            string json = await entry.ReadAllText();
            IReadOnlyDictionary<string, string>? assetObject = JsonConvert.DeserializeObject<Dictionary<string, string>?>(json)?.AsReadOnly();
            return new LanguageAssetHandle(new LocalizationData(assetObject ?? new ReadOnlyDictionary<string, string>(new Dictionary<string, string>())), entry, metaData);
        }

        protected override UniTask OnBeginAssetLoop()
        {
            calculatedAsset.Clear();
            return UniTask.CompletedTask;
        }

        protected override UniTask OnAssetLoop(Identifier identifier, IIOEntry entry, LanguageAssetHandle assetHandle)
        {
            RecordAssetHandle(identifier, assetHandle);

            IReadOnlyDictionary<string, string> localizations = assetHandle.assetObject.localizations;
            if (calculatedAsset.TryGetValue(identifier.path, out Dictionary<Identifier, string>? value))
            {
                calculatedAsset[identifier.path] = value
                    .Concat
                    (
                        localizations
                            .AsDictionary(x => new Identifier(identifier.nameSpace, x.Key), x => x.Value)
                    )
                    .GroupBy(x => x.Key)
                    .ToDictionary(x => x.Key, x => x.First().Value);
            }
            else
                calculatedAsset.Add(identifier.path, localizations.ToDictionary(x => new Identifier(identifier.nameSpace, x.Key), x => x.Value));
            
            return UniTask.CompletedTask;
        }
    }
}