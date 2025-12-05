#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Booting;
using RuniOS.IO;
using RuniOS.Linq;
using System.Collections.Immutable;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Languages
{
    public sealed class LanguageAssetRegistry : SimpleAssetRegistry
    {
        public override string registryName => "lang";

        public override Type handleType => typeof(LanguageAssetHandle);
        public override Type scopeType => typeof(LanguageAssetScope);

        public override WildcardPatterns assetFilter => WildcardPatterns.jsonFileFilter;

        public IReadOnlyDictionary<string, IReadOnlyDictionary<Identifier, string>> calculatedAsset { get; }
        readonly Dictionary<string, IReadOnlyDictionary<Identifier, string>> _calculatedAsset = new();

        public LanguageAssetRegistry() => calculatedAsset = _calculatedAsset.AsReadOnly();
        
        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => ResourceManager.RegisterAssetRegistry<LanguageAssetRegistry>();

        protected override AssetHandle CreateHandle(IOHandler ioHandler, ImmutableArray<byte> md5Hash) => new LanguageAssetHandle(ioHandler, md5Hash);

        protected override UniTask OnBeginAssetLoop()
        {
            _calculatedAsset.Clear();
            return UniTask.CompletedTask;
        }

        protected override async UniTask OnAssetLoop(Identifier identifier, IOHandler ioHandler, AssetHandle assetHandle)
        {
            using LanguageAssetScope? scope = (LanguageAssetScope?)await assetHandle.GetScope();
            
            if (scope != null)
            {
                if (_calculatedAsset.TryGetValue(identifier.path, out IReadOnlyDictionary<Identifier, string>? value))
                {
                    _calculatedAsset[identifier.path] = value
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
                    _calculatedAsset.Add(identifier.path, scope.texts.ToDictionary(x => new Identifier(identifier.nameSpace, x.Key), x => x.Value));
            }
                                    
            RecordAssetHandle(identifier, assetHandle);
        }
    }
}