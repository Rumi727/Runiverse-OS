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



        public static async UniTask Reload(IProgress<float>? progress = null)
        {
            if (isLoading)
            {
                await UniTask.WaitWhile(() => isLoading);
                return;
            }
            
            currentTask = new AsyncTask("runios:resource.loading.title", "runios:resource.loading.description");
            reloadStartEvent?.SafeInvoke(currentTask);
            
            try
            {
                await ResourcePack.GetDefaultPack();
                await ResourcePack.ReloadAll();

                ReadOnlySet<AssetRegistry> assetRegistries = AssetRegistryManager.GetAll();

                UniTask[] uniTasks = new UniTask[assetRegistries.Count];
                float[] assetRegistryProgresses = new float[assetRegistries.Count];

                int index = 0;
                foreach (var assetRegistry in assetRegistries)
                {
                    int targetIndex = index;
                    
                    uniTasks[index] = UniTask.Defer(() => RegistryReload(assetRegistry, targetIndex));
                    index++;
                    
                    async UniTask RegistryReload(AssetRegistry assetRegistry, int targetIndex)
                    {
                        try
                        {
                            await assetRegistry.Reload
                            (
                                ResourcePack.enabledPacks.Where(x => x.isValid),
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



        public static AssetHandle<T>? GetHandle<T>(ResourceKey key) => GetHandle(key) as AssetHandle<T>;
        public static IAssetHandle? GetHandle(ResourceKey key)
        {
            AssetRegistry? registry = AssetRegistryManager.Get(key.registryId);
            if (registry?.assetHandles.TryGetValue(key.assetId, out IAssetHandle? handle) ?? false)
                return handle;

            return null;
        }
        
        
        public static AssetHandle<T>? GetHandle<T>(Identifier identifier)
        {
            AssetRegistry? registry = AssetRegistryManager.GetDefaultForAsset<T>();
            if (registry?.assetHandles.TryGetValue(identifier, out IAssetHandle? handle) ?? false)
                return handle as AssetHandle<T>;

            return null;
        }
        
        
        public static async UniTask<AssetScope<T>?> LoadScopeAsync<T>(Identifier identifier)
        {
            AssetRegistry? registry = AssetRegistryManager.GetDefaultForAsset<T>();
            if (registry == null)
                return null;

            if (registry.assetHandles.TryGetValue(identifier, out IAssetHandle handle))
            {
                if (handle is AssetHandle<T> typedHandle)
                    return await typedHandle.GetScope();
            }
            
            return null;
        }

        public static async UniTask<AssetScope<T>?> LoadScopeAsync<T>(ResourceKey key)
        {
            AssetRegistry? registry = AssetRegistryManager.Get(key.registryId);
            if (registry == null)
                return null;

            if (registry.assetHandles.TryGetValue(key.assetId, out IAssetHandle handle))
            {
                if (handle is AssetHandle<T> typedHandle)
                    return await typedHandle.GetScope();
            }
            
            return null;
        }
    }
}