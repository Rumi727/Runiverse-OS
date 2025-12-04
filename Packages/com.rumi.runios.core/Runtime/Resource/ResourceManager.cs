#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Reflection;
using RuniOS.Tasks;

namespace RuniOS.Resource
{
    public static class ResourceManager
    {
        static readonly AssetRegistryList _assetRegistries = new();
        public static ReadOnlyAssetRegistryList assetRegistries { get; } = new ReadOnlyAssetRegistryList(_assetRegistries);



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

                UniTask[] uniTasks = new UniTask[assetRegistries.Count];
                float[] assetRegistryProgresses = new float[assetRegistries.Count];
                
                for (int i = 0; i < uniTasks.Length; i++)
                {
                    AssetRegistry assetRegistry = assetRegistries[i];
                    int targetIndex = i;
                    
                    uniTasks[i] = UniTask.Defer(() => RegistryReload(assetRegistry, targetIndex));
                    
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
        
        

        public static void RegisterAssetRegistry<T>() where T : AssetRegistry, new() => RegisterAssetRegistry(typeof(T));
        public static void RegisterAssetRegistry(Type registryType)
        {
            if (registryType.IsAbstract)
                throw new ArgumentException($"Type '{registryType.FullName}' cannot be abstract.", nameof(registryType));

            if (!typeof(AssetRegistry).IsAssignableFrom(registryType))
                throw new ArgumentException($"Type '{registryType.FullName}' must inherit from AssetRegistry.", nameof(registryType));

            if (!registryType.HasDefaultConstructor())
                throw new ArgumentException($"Type '{registryType.FullName}' must have a public parameterless constructor.", nameof(registryType));

            if (isLoading)
                throw new InvalidOperationException("The registry is still reloading!");
            
            _assetRegistries.Add((AssetRegistry)Activator.CreateInstance(registryType));
        }

        public static void UnregisterAssetRegistry<T>() where T : AssetRegistry, new() => UnregisterAssetRegistry(typeof(T));
        public static void UnregisterAssetRegistry(Type type)
        {
            if (isLoading)
                throw new InvalidOperationException("The registry is still reloading!");
            
            _assetRegistries.RemoveOfType(type);
        }



        public static TRegistry? GetRegistry<TRegistry>() where TRegistry : AssetRegistry => GetRegistry(typeof(TRegistry)) as TRegistry;
        public static AssetRegistry? GetRegistry(Type registryType)
        {
            assetRegistries.FindOfType(registryType, out AssetRegistry? value);
            return value;
        }
    
    
    
        public static AssetRegistry? GetRegistry(RegistryType type)
        {
            assetRegistries.FindOfName(type, out AssetRegistry? value);
            return value;
        }



        public static IReadOnlyDictionary<Identifier, AssetHandle>? GetAllAssetHandles<THandle>() where THandle : AssetHandle => GetAllAssetHandles(typeof(THandle));
        
        public static IReadOnlyDictionary<Identifier, AssetHandle>? GetAllAssetHandles(Type handleType)
        {
            if (assetRegistries.FindOfHandle(handleType, out AssetRegistry? assetRegistry))
                return assetRegistry.assetHandles;

            return null;
        }
        
        public static THandle? GetAssetHandle<THandle>(Identifier identifier) where THandle : AssetHandle => GetAssetHandle(typeof(THandle), identifier) as THandle;

        public static AssetHandle? GetAssetHandle(Type handleType, Identifier identifier)
        {
            if (GetAllAssetHandles(handleType)?.TryGetValue(identifier, out AssetHandle? handle) ?? false)
                return handle;

            return null;
        }
        
        public static async UniTask<AssetScope?> GetAssetScope<TScope>(Identifier identifier) where TScope : AssetScope => await GetAssetScope(typeof(TScope), identifier);

        public static UniTask<AssetScope?> GetAssetScope(Type scopeType, Identifier identifier)
        {
            if (assetRegistries.FindOfScope(scopeType, out AssetRegistry? assetRegistry) && assetRegistry.assetHandles.TryGetValue(identifier, out AssetHandle? handle))
                return handle.GetScope();

            return UniTask.FromResult<AssetScope?>(null);
        }
    }
}