using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.Booting;
using RuniOS.IO;
using RuniOS.Sounds;
using System.Text.RegularExpressions;
using UnityEngine.Scripting;

namespace RuniOS.Resource.Sounds
{
    public sealed class SoundAssetRegistry : AssetRegistry<InstanceAssetHandle<SoundClipRef>>
    {
        public const string jsonFileName = "sounds.json";

        public static SoundAssetRegistry instance => AssetRegistryManager.Get<SoundAssetRegistry>() ?? new SoundAssetRegistry();
        
        public override Identifier registryId => new Identifier("runios", "sounds");
        
        public override bool isDefault => true;

        public override Type assetType => typeof(SoundClipRef);

        public override bool isLoading => _isLoading;
        bool _isLoading;
        
        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => AssetRegistryManager.Register<SoundAssetRegistry>();
        
        public override async UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null)
        {
            if (isLoading)
            {
                await UniTask.WaitWhile(() => isLoading);
                return;
            }

            _isLoading = true;
            BeginTracking();

            try
            {
                progress?.Report(0);
                
                List<UniTask> uniTasks = new List<UniTask>();
                int count = 0;
                
                foreach (var resourcePack in resourcePacks)
                {
                    foreach ((string nameSpace, IIOEntry jsonEntry) in resourcePack.GetNamespaceHandlers()
                                 .Select(x => (x.name, x.CreateChild(jsonFileName))))
                    {
                        uniTasks.Add(UniTask.Defer(Method));
                        
                        async UniTask Method()
                        {
                            try
                            {
                                if (!await jsonEntry.FileExists())
                                    return;
                                
                                Dictionary<string, ResourceKey>? sounds = JsonConvert.DeserializeObject<Dictionary<string, ResourceKey>>(await jsonEntry.ReadAllText());
                                if (sounds == null)
                                    return;
                                
                                foreach (var sound in sounds)
                                    RecordAssetHandle(new Identifier(nameSpace, sound.Key), new InstanceAssetHandle<SoundClipRef>(sound.Value));
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"An exception occurred while loading {jsonEntry.fullPath} resources from the resource pack {resourcePack.identifier}. The exception is: {e}");
                            }

                            // UniTask.WhenAll이 대기하는 작업의 진행률 보고
                            progress?.Report((float)++count / uniTasks.Count);
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

        static readonly Regex pathRegex = new Regex($"^asset/[^/]+/{Regex.Escape(jsonFileName)}$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        public override bool IsMatch(FilePath relativePath) => pathRegex.IsMatch(relativePath);
    }
}