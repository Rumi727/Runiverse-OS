#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Collections.Generic;
using RuniOS.Tasks;

namespace RuniOS.Resource
{
    public static class ResourceManager
    {
        /// <summary>
        /// 에디터에서 리소스 레지스트리가 미리 로드되었는지 여부를 나타냅니다.
        /// 런타임에는 리소스 레지스트리를 처음으로 로드한 이후라면 true입니다.
        /// </summary>
        public static bool isPreloaded { get; private set; } = false;

        public static bool isLoading => currentTask != null;
        public static AsyncTask? currentTask { get; private set; } = null;
        
        public static event Action<AsyncTask>? reloadStartEvent;
        public static event Action? reloadCompletionEvent;

        static bool reloadRequested;
        public static async UniTask Reload(IProgress<float>? progress = null)
        {
            reloadRequested = true;

            if (isLoading)
            {
                await UniTask.WaitWhile(() => isLoading);
                return;
            }
            
            currentTask = new AsyncTask("runios:resource.loading.title", "runios:resource.loading.description");
            reloadStartEvent?.SafeInvoke(currentTask);
            
            try
            {
                while (reloadRequested)
                {
                    reloadRequested = false;

                    await ResourcePack.GetDefaultPack();
                    await ResourcePack.ReloadAll();

                    ReadOnlySet<IAssetRegistry> assetRegistries = AssetRegistryManager.GetAll();

                    UniTask[] uniTasks = new UniTask[assetRegistries.Count];
                    float[] assetRegistryProgresses = new float[assetRegistries.Count];

                    int index = 0;
                    foreach (var assetRegistry in assetRegistries)
                    {
                        int targetIndex = index;

                        uniTasks[index] = UniTask.Defer(() => RegistryReload(assetRegistry, targetIndex));
                        index++;

                        async UniTask RegistryReload(IAssetRegistry assetRegistry, int targetIndex)
                        {
                            try
                            {
                                await assetRegistry.Reload
                                (
                                    ResourcePack.GetEnabledPacksSnapshot(),
                                    Progress.Create<float>(x =>
                                    {
                                        assetRegistryProgresses[targetIndex] = x;

                                        float value = assetRegistryProgresses.Sum() / assetRegistryProgresses.Length;
                                        if (currentTask != null)
                                            currentTask.progress.Value = value;

                                        progress?.Report(value);
                                    })
                                );
                            }
                            catch (Exception e)
                            {
                                Debug.LogError($"An exception occurred while loading the resource pack registry {assetRegistry.GetType().Name}. The exception is: {e}", nameof(ResourceManager));
                            }
                        }
                    }

                    await UniTask.WhenAll(uniTasks);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                try
                {
                    currentTask.progress.Value = 1;
                    progress?.Report(1);
                    
                    currentTask.Dispose();
                    currentTask = null;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                
                isPreloaded = true;
                reloadCompletionEvent.SafeInvoke();
            }
        }



        public static IAssetHandle<T>? GetHandle<T>(ResourceKey key) => GetHandle(key) as IAssetHandle<T>;
        public static IAssetHandle? GetHandle(ResourceKey key) => AssetRegistryManager.Get(key.registryId)?[key.assetId];


        public static IAssetHandle<T>? GetHandle<T>(Identifier identifier) => AssetRegistryManager.GetDefaultForAsset<T>()?[identifier] as IAssetHandle<T>;
        public static UniTask<IAssetScope<T>?> LoadScopeAsync<T>(Identifier identifier) => GetHandle<T>(identifier)?.GetScope() ?? UniTask.FromResult<IAssetScope<T>?>(null);

        public static UniTask<IAssetScope<T>?> LoadScopeAsync<T>(ResourceKey key) => GetHandle<T>(key)?.GetScope() ?? UniTask.FromResult<IAssetScope<T>?>(null);
    }
}