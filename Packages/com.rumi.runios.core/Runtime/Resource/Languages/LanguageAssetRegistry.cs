#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Booting;
using RuniOS.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Languages
{
    public sealed class LanguageAssetRegistry : SimpleAssetRegistry
    {       
        public override string registryName => "lang";

        public override Type handleType => typeof(LanguageAssetHandle);
        public override Type scopeType => typeof(LanguageAssetScope);

        public override WildcardPatterns assetFilter => WildcardPatterns.jsonFileFilter;

        public IReadOnlyDictionary<string, IReadOnlyDictionary<Identifier, string>> preloadedAsset { get; }
        readonly Dictionary<string, IReadOnlyDictionary<Identifier, string>> _preloadedAsset = new();

        public LanguageAssetRegistry() => preloadedAsset = _preloadedAsset.AsReadOnly();
        
        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => ResourceManager.RegisterAssetRegistry<LanguageAssetRegistry>();

        protected override AssetHandle CreateHandle(IOHandler ioHandler, string md5Hash) => new LanguageAssetHandle(ioHandler, md5Hash);

        protected override async UniTask OnAssetLoop(Identifier identifier, IOHandler ioHandler, AssetHandle assetHandle)
        {
            using LanguageAssetScope? scope = (LanguageAssetScope?)await assetHandle.GetScope();
            
            if (scope != null)
            {
                if (_preloadedAsset.TryGetValue(identifier, out IReadOnlyDictionary<Identifier, string>? value))
                {
                    _preloadedAsset[identifier] = value
                        .Concat
                        (
                            scope.texts
                                .AsDictionary(x => new Identifier(identifier.nameSpace, x.Key), x => x.Value)
                        )
                        .GroupBy(x => x.Key)
                        .ToDictionary(x => x.Key, x => x.First().Value)
                        .AsReadOnly();
                }
                else
                    _preloadedAsset.Add(identifier, scope.texts.ToDictionary(x => new Identifier(identifier.nameSpace, x.Key), x => x.Value));
            }
                                    
            RecordAssetHandle(identifier, assetHandle);
        }
    }
}
