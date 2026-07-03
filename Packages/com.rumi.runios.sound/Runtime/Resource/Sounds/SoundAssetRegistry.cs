#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.Booting;
using RuniOS.IO;
using RuniOS.Sounds;
using RuniOS.Tasks;
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

        public override bool isLoading => reloadGate.isRunning;

        readonly AsyncReloadGate reloadGate = new();

        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => AssetRegistryManager.Register<SoundAssetRegistry>();

        public override UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null)
        {
            ResourcePack[] resourcePackSnapshot = resourcePacks.ToArray();
            return reloadGate.Run(passProgress => ReloadCore(resourcePackSnapshot, passProgress), progress);
        }

        async UniTask ReloadCore(ResourcePack[] resourcePacks, IProgress<float>? progress)
        {
            BeginTracking();

            try
            {
                progress.SafeReport(0);

                List<UniTask> uniTasks = new List<UniTask>();
                int count = 0;

                foreach (var resourcePack in resourcePacks)
                {
                    foreach ((string nameSpace, IONode jsonNode) in resourcePack.GetNamespaceNodes()
                                 .Select(x => (x.name, x.CreateChild(jsonFileName))))
                    {
                        if (await jsonNode.file.GetEntry() == null)
                            return;

                        uniTasks.Add(UniTask.Defer(Method));

                        async UniTask Method()
                        {
                            try
                            {
                                Dictionary<string, ResourceKey>? sounds = JsonConvert.DeserializeObject<Dictionary<string, ResourceKey>>(await jsonNode.file.ReadAllText());
                                if (sounds == null)
                                    return;

                                foreach (var sound in sounds)
                                    RecordAssetHandle(new Identifier(nameSpace, sound.Key), new InstanceAssetHandle<SoundClipRef>(sound.Value));
                            }
                            catch (Exception e)
                            {
                                Debug.RuntimeLogError($"An exception occurred while loading {jsonNode.path} resources from the resource pack {resourcePack.identifier}. The exception is: {e}");
                            }

                            // UniTask.WhenAll이 대기하는 작업의 진행률 보고
                            progress.SafeReport((float)++count / uniTasks.Count);
                        }
                    }
                }

                await UniTask.WhenAll(uniTasks);
            }
            finally
            {
                progress.SafeReport(1);

                EndTracking();
            }
        }
    }
}