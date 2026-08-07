#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Collections.Generic;
using RuniOS.Linq;
using RuniOS.Tasks;
using RuniOS.Texts;

namespace RuniOS.Resource
{
    public static class ResourceManager
    {
        /// <summary>
        /// 에디터에서 리소스 레지스트리가 미리 로드되었는지 여부를 나타냅니다.
        /// 런타임에는 리소스 레지스트리를 처음으로 로드한 이후라면 true입니다.
        /// </summary>
        public static bool isPreloaded { get; private set; } = false;

        public static bool isLoading => reloadGate.isRunning;
        public static AsyncTask? currentTask { get; private set; } = null;
        
        public static event Action<AsyncTask>? reloadStartEvent;

        public static event Action? preReloadCompletionEvent;
        public static event Action? reloadCompletionEvent;

        static readonly HashSet<IReloadable> _reloadables = [];
        public static ReadOnlySet<IReloadable> reloadables { get; } = _reloadables.AsReadOnly();

        static readonly AsyncReloadGate reloadGate = new();

        public static void AttachReloadable(IReloadable reloadable) => _reloadables.Add(reloadable);
        public static void DetachReloadable(IReloadable reloadable) => _reloadables.Remove(reloadable);

        public static UniTask Reload(IProgress<float>? progress = null) => reloadGate.Run(ReloadCore, progress);

        static async UniTask ReloadCore(IProgress<float>? progress)
        {
            currentTask = new AsyncTask(Text.Local("runios:resource.loading.title"), Text.Local("runios:resource.loading.description"));
            reloadStartEvent?.SafeInvoke(currentTask);

            try
            {
                progress.SafeReport(0);

                await ResourcePack.ReloadAll();

                ReadOnlySet<IAssetRegistry> assetRegistries = AssetRegistryManager.GetAll();

                UniTask[] uniTasks = new UniTask[assetRegistries.Count];
                float[] assetRegistryProgresses = new float[assetRegistries.Count];

                ResourcePack[] resourcePacks = ResourcePack.GetEnabledPacksSnapshot();

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
                                resourcePacks,
                                Progress.Create<float>(x =>
                                {
                                    assetRegistryProgresses[targetIndex] = x;

                                    float value = assetRegistryProgresses.Sum() / assetRegistryProgresses.Length;
                                    if (currentTask != null)
                                        currentTask.progress.Value = value;

                                    progress.SafeReport(value);
                                })
                            );
                        }
                        catch (Exception e)
                        {
                            Debug.RuntimeLogError($"An exception occurred while loading the resource pack registry {assetRegistry.GetType().Name}. The exception is: {e}", nameof(ResourceManager));
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
                progress.SafeReport(1);

                currentTask.progress.Value = 1;

                currentTask.Dispose();
                currentTask = null;

                isPreloaded = true;

                preReloadCompletionEvent.SafeInvoke();
                reloadCompletionEvent.SafeInvoke();

                foreach (var reloadable in reloadables)
                {
                    try
                    {
                        reloadable.Reload().Forget();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }



        public static IAssetHandle<T>? GetHandle<T>(ResourceKey key) where T : notnull => GetHandle(key) as IAssetHandle<T>;
        public static IAssetHandle? GetHandle(ResourceKey key) => AssetRegistryManager.Get(key.registryId)?[key.assetId];


        public static IAssetHandle<T>? GetHandle<T>(Identifier identifier) where T : notnull => AssetRegistryManager.GetDefaultForAsset<T>()?[identifier] as IAssetHandle<T>;
        public static UniTask<IAssetScope<T>?> LoadScopeAsync<T>(Identifier identifier) where T : notnull => GetHandle<T>(identifier)?.GetScope() ?? UniTask.FromResult<IAssetScope<T>?>(null);

        public static UniTask<IAssetScope<T>?> LoadScopeAsync<T>(ResourceKey key) where T : notnull => GetHandle<T>(key)?.GetScope() ?? UniTask.FromResult<IAssetScope<T>?>(null);
    }
}