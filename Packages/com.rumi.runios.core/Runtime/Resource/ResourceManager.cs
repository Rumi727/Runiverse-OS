#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.Resource
{
    public static class ResourceManager
    {
        /*
         * TODO
         * 임시
         */
        internal static readonly Dictionary<PackIdentifier, ResourcePack> internalLoadedResourcePacks = new();
        public static IReadOnlyDictionary<PackIdentifier, ResourcePack> loadedResourcePacks { get; } = internalLoadedResourcePacks.AsReadOnly();

        /*
         * TODO
         * 임시
        */
        internal static readonly HashSet<PackIdentifier> internalEnabledPackIdentifiers = new();
        public static ReadOnlyHashSet<PackIdentifier> enabledPackIdentifiers { get; } = new(internalEnabledPackIdentifiers);
        
        static readonly AssetRegistryList _assetRegistries = new();
        public static ReadOnlyAssetRegistryList assetRegistries { get; } = new ReadOnlyAssetRegistryList(_assetRegistries);



        /// <summary>
        /// 에디터에서 리소스 레지스트리가 미리 로드되었는지 여부를 나타냅니다.
        /// 런타임에는 리소스 레지스트리를 처음으로 로드한 이후라면 true입니다.
        /// </summary>
        public static bool isPreloaded { get; private set; } = false;
        public static bool isLoading { get; private set; } = false;
        
        public static event Action? reloadCompletionEvent;



        public static async UniTask Reload(IProgress<float>? progress = null)
        {
            if (isLoading)
            {
                await UniTask.WaitWhile(() => isLoading);
                return;
            }

            isLoading = true;
            
            try
            {
                await ResourcePack.GetDefaultPack();
                
                EnablePack(ResourcePack.defaultPackIdentifier);

                UniTask[] uniTasks = new UniTask[assetRegistries.Count];
                float[] assetRegistryProgresses = new float[assetRegistries.Count];
                
                for (int i = 0; i < uniTasks.Length; i++)
                {
                    uniTasks[i] = RegistryReload(assetRegistries[i], i);
                    
                    async UniTask RegistryReload(AssetRegistry assetRegistry, int targetIndex)
                    {
                        try
                        {
                            await UniTask.Yield();
                            await assetRegistry.Reload
                            (
                                loadedResourcePacks
                                    .Where(x => enabledPackIdentifiers.Contains(x.Key))
                                    .Select(x => x.Value),
                                Progress.Create<float>(x =>
                                {
                                    assetRegistryProgresses[targetIndex] = x;
                                    progress?.Report(assetRegistryProgresses.Sum() / assetRegistryProgresses.Length);
                                })
                            );
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"An exception occurred while loading the resource pack registry {assetRegistry.GetType().Name}. The exception is: {e}");
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
                    progress?.Report(1);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                
                isLoading = false;
                isPreloaded = true;
            }
            
            reloadCompletionEvent.SafeInvoke();
        }
        
        

        public static UniTask RegisterAssetRegistry<T>() where T : AssetRegistry, new() => RegisterAssetRegistry(typeof(T));
        public static async UniTask RegisterAssetRegistry(Type registryType)
        {
            if (registryType.IsAbstract)
                throw new ArgumentException($"Type '{registryType.FullName}' cannot be abstract.", nameof(registryType));

            if (!registryType.IsSubtypeOf(typeof(AssetRegistry)))
                throw new ArgumentException($"Type '{registryType.FullName}' must inherit from AssetRegistry.", nameof(registryType));

            if (!registryType.HasDefaultConstructor())
                throw new ArgumentException($"Type '{registryType.FullName}' must have a public parameterless constructor.", nameof(registryType));
            
            if (isLoading)
                await UniTask.WaitWhile(() => isLoading);
            
            _assetRegistries.Add((AssetRegistry)Activator.CreateInstance(registryType));
        }

        public static void UnregisterAssetRegistry<T>() where T : AssetRegistry, new() => UnregisterAssetRegistry(typeof(T));
        public static void UnregisterAssetRegistry(Type type) => _assetRegistries.RemoveOfType(type);



        public static TRegistry? GetRegistry<TRegistry>() where TRegistry : AssetRegistry => GetRegistry(typeof(TRegistry)) as TRegistry;
        public static AssetRegistry? GetRegistry(Type registryType)
        {
            assetRegistries.FindOfType(registryType, out AssetRegistry? value);
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
        
        public static void EnablePack(PackIdentifier identifier) => internalEnabledPackIdentifiers.Add(identifier);
        public static void DisablePack(PackIdentifier identifier) => internalEnabledPackIdentifiers.Remove(identifier);
    }
}
