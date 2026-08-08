#nullable enable
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using RuniOS.IO;
using RuniOS.Linq;
using RuniOS.Localizations;
using RuniOS.Tasks;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Resource.Languages
{
    public sealed partial class LanguageAssetRegistry : AssetRegistry<InstanceAssetHandle<LocalizationData>>
    {
        public override Identifier registryId => new Identifier("runios", "lang");

        public override bool isDefault => true;

        public override Type assetType => typeof(LocalizationData);

        public override bool isSupportedImportData => false;

        public override bool isLoading => reloadGate.isRunning;

        Dictionary<Identifier, IReadOnlyDictionary<string, string>> calculatedAsset = new();

        [OnCodeLoaded]
        static void OnCodeLoaded() => AssetRegistryManager.Register<LanguageAssetRegistry>();

        [OnCodeUnloading]
        static void OnCodeUnloading() => AssetRegistryManager.Unregister<LanguageAssetRegistry>();

        readonly AsyncReloadGate reloadGate = new();
        public override UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null) => reloadGate.Run(progress => ReloadCore(resourcePacks, progress), progress);

        async UniTask ReloadCore(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress)
        {
            BeginTracking();

            try
            {
                progress.SafeReport(0);

                Dictionary<Identifier, Dictionary<string, string>> mergedData = new();

                {
                    List<UniTask> uniTasks = [];
                    int count = 0;

                    foreach (var resourcePack in resourcePacks)
                    {
                        foreach (var namespaceNode in resourcePack.GetNamespaceNodes())
                        {
                            IONode registryNode = namespaceNode.CreateChild(registryId.path);
                            if (!await registryNode.dir.Exists())
                                continue;

                            await foreach (IOEntry fileEntry in registryNode.dir.GetAllFiles(WildcardPatterns.jsonFileFilter))
                            {
                                uniTasks.Add(UniTask.Defer(Method));

                                async UniTask Method()
                                {
                                    try
                                    {
                                        RuniPath path = fileEntry.path.GetRelativePath(registryNode.path).GetPathWithoutExtension();
                                        Identifier identifier = new Identifier(namespaceNode.name, path);

                                        string json = await registryNode.Bind(fileEntry).file.ReadAllText();
                                        Dictionary<string, string>? newData = JsonConvert.DeserializeObject<Dictionary<string, string>?>(json);
                                        if (newData == null)
                                            return;

                                        if (mergedData.TryGetValue(identifier, out Dictionary<string, string>? oldData))
                                        {
                                            foreach (var data in newData)
                                                oldData.TryAdd(data.Key, data.Value);
                                        }
                                        else
                                            mergedData.Add(identifier, newData);
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.RuntimeLogError($"An exception occurred while loading {fileEntry.path} resources from the resource pack {resourcePack.identifier}. The exception is: {e}");
                                    }
                                    finally
                                    {
                                        // UniTask.WhenAll이 대기하는 작업의 진행률 보고
                                        progress.SafeReport((float)++count / (uniTasks.Count * 2));
                                    }
                                }
                            }
                        }
                    }

                    await UniTask.WhenAll(uniTasks);
                }

                {
                    int count = 0;
                    foreach (var data in mergedData)
                    {
                        RecordAssetHandle(data.Key, new InstanceAssetHandle<LocalizationData>(new LocalizationData(data.Value.AsReadOnly())));
                        // UniTask.WhenAll이 대기하는 작업의 진행률 보고
                        progress.SafeReport(0.5f + ((float)++count / (mergedData.Count * 2)));
                    }
                }

                calculatedAsset = mergedData.ToDictionary(x => x.Key, IReadOnlyDictionary<string, string> (x) => x.Value.AsReadOnly());
            }
            catch (Exception e)
            {
                Debug.RuntimeLogError($"An unexpected exception occurred while reloading resources. The exception is: {e}");
            }
            finally
            {
                progress.SafeReport(1);

                EndTracking();
            }
        }

        public IEnumerable<string> GetAllLanguageCodes() => calculatedAsset.Select(x => x.Key.path.value).Distinct();

        public IReadOnlyDictionary<string, string>? GetAsset(Identifier identifier) => calculatedAsset.GetValueOrDefault(identifier);
    }
}