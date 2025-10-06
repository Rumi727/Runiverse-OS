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
    public sealed class LanguageAssetRegistry : AssetRegistry
    {       
        public override string registryName => "lang";

        public override Type handleType => typeof(LanguageAssetHandle);
        public override Type scopeType => typeof(LanguageAssetScope);

        public override bool isLoading => _isLoading;
        bool _isLoading;

        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> preloadedAsset { get; }
        readonly Dictionary<string, IReadOnlyDictionary<string, string>> _preloadedAsset = new();

        public LanguageAssetRegistry() => preloadedAsset = _preloadedAsset.AsReadOnly();

        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => ResourceManager.RegisterAssetRegistry<LanguageAssetRegistry>();
        
        public override async UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null)
        {
            _isLoading = true;
            BeginTracking();
            
            try
            {
                _preloadedAsset.Clear();
            
                progress?.Report(0);

                List<UniTask> uniTasks = new List<UniTask>();
                int count = 0;
                
                foreach (var resourcePack in resourcePacks)
                {
                    foreach ((string nameSpace, IOHandler registryHandler) in await GetRegistryFolder(resourcePack))
                    {
                        foreach (var ioHandler in await registryHandler.GetFileHandlers(WildcardPatterns.jsonFileFilter))
                        {
                            uniTasks.Add(Method());

                            async UniTask Method()
                            {
                                await UniTask.Yield();
                                
                                try
                                {
                                    string name = ioHandler.fullPath.GetFileNameWithoutExtension();
                                    Identifier identifier = new Identifier(nameSpace, name);
                                    LanguageAssetHandle handle = new LanguageAssetHandle(ioHandler);
                                    using LanguageAssetScope? scope = (LanguageAssetScope?)await handle.GetScope();

                                    if (scope != null)
                                    {
                                        if (_preloadedAsset.TryGetValue(name, out IReadOnlyDictionary<string, string>? value))
                                            _preloadedAsset[name] = value.Concat(scope.texts).GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.First().Value).AsReadOnly();
                                        else
                                            _preloadedAsset.Add(name, scope.texts);
                                    }
                                    
                                    RecordAssetHandle(identifier, handle);
                                }
                                catch (Exception e)
                                {
                                    Debug.Log($"An exception occurred while loading {ioHandler.fullPath} resources from the resource pack {resourcePack.identifier}. The exception is: {e}");
                                }

                                count++;
                                progress?.Report((float)count / uniTasks.Count);
                            }
                        }
                    }
                }

                await UniTask.WhenAll(uniTasks);
            }
            finally
            {
                try
                {
                    progress?.Report(1);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                
                EndTracking();
                _isLoading = false;
            }
        }
    }
}
