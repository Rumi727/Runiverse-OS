#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.IO;
using RuniOS.Sounds;
using RuniOS.Tasks;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.Sounds
{
    public sealed partial class SoundAssetRegistry : AssetRegistry<InstanceAssetHandle<SoundClipRef>>
    {
        /// <inheritdoc cref="registryId" />
        public static readonly Identifier id = new Identifier("runios", "sounds");
        public const string jsonFileName = "sounds.json";

        public override Identifier registryId => id;

        public override bool isSupportedImportData => false;

        public override int priority => 100;

        public override Type assetType => typeof(SoundClipRef);

        public override bool isLoading => reloadGate.isRunning;

        readonly AsyncReloadGate reloadGate = new();

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<SoundAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<SoundAssetRegistry>();

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
                        if (!await jsonNode.file.Exists())
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