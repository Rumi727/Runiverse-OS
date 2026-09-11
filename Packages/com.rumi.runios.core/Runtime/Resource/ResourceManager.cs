#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Collections.Generic;
using RuniOS.Linq;
using RuniOS.Tasks;
using RuniOS.Texts;

namespace RuniOS.Resource
{
    /// <summary>
    /// Manages resource-pack loading and reloadable resource registries.<br/>
    /// 리소스 팩 로드와 다시 로드 가능한 리소스 레지스트리를 관리합니다.
    /// </summary>
    public static class ResourceManager
    {
        /// <summary>
        /// Gets whether the resource registries have completed their initial preload.<br/>
        /// 리소스 레지스트리가 최초 사전 로드를 완료했는지 여부를 가져옵니다.
        /// <br/><br/>
        /// In the editor, this indicates whether the registries were preloaded. At runtime, it becomes <see langword="true"/> after the first reload completes.<br/>
        /// 에디터에서는 레지스트리의 사전 로드 여부를 나타내며, 런타임에서는 첫 번째 다시 로드가 완료된 후 <see langword="true"/>가 됩니다.
        /// </summary>
        public static bool isPreloaded { get; private set; } = false;

        /// <summary>
        /// Gets whether a resource reload is currently running.<br/>
        /// 현재 리소스 다시 로드가 실행 중인지 여부를 가져옵니다.
        /// </summary>
        public static bool isLoading => reloadGate.isRunning;

        /// <summary>
        /// Gets the current resource reload task, or <see langword="null"/> when no reload is running.<br/>
        /// 현재 리소스 다시 로드 작업을 가져오며, 실행 중인 다시 로드가 없으면 <see langword="null"/>을 반환합니다.
        /// </summary>
        public static AsyncTask? currentTask { get; private set; } = null;

        /// <summary>
        /// Occurs when a resource reload starts.<br/>
        /// 리소스 다시 로드가 시작될 때 발생합니다.
        /// </summary>
        public static event Action<AsyncTask>? reloadStartEvent;

        /// <summary>
        /// Occurs immediately before a resource reload completes.<br/>
        /// 리소스 다시 로드가 완료되기 직전에 발생합니다.
        /// </summary>
        public static event Action? preReloadCompletionEvent;

        /// <summary>
        /// Occurs when a resource reload completes.<br/>
        /// 리소스 다시 로드가 완료될 때 발생합니다.
        /// </summary>
        public static event Action? reloadCompletionEvent;

        static readonly HashSet<IReloadable> _reloadables = [];

        /// <summary>
        /// Gets the reloadable resources attached to this manager.<br/>
        /// 이 관리자에 연결된 다시 로드 가능한 리소스를 가져옵니다.
        /// </summary>
        public static ReadOnlySet<IReloadable> reloadables { get; } = _reloadables.AsReadOnly();

        static readonly AsyncReloadGate reloadGate = new();

        /// <summary>
        /// Attaches a reloadable resource to this manager.<br/>
        /// 다시 로드 가능한 리소스를 이 관리자에 연결합니다.
        /// </summary>
        /// <param name="reloadable">
        /// The reloadable resource to attach.<br/>
        /// 연결할 다시 로드 가능한 리소스입니다.
        /// </param>
        public static void AttachReloadable(IReloadable reloadable) => _reloadables.Add(reloadable);

        /// <summary>
        /// Detaches a reloadable resource from this manager.<br/>
        /// 다시 로드 가능한 리소스를 이 관리자에서 연결 해제합니다.
        /// </summary>
        /// <param name="reloadable">
        /// The reloadable resource to detach.<br/>
        /// 연결 해제할 다시 로드 가능한 리소스입니다.
        /// </param>
        public static void DetachReloadable(IReloadable reloadable) => _reloadables.Remove(reloadable);

        /// <summary>
        /// Reloads all resource packs and attached registries.<br/>
        /// 모든 리소스 팩과 연결된 레지스트리를 다시 로드합니다.
        /// </summary>
        /// <param name="progress">
        /// The object that receives overall progress updates, or <see langword="null"/> when progress reporting is not required.<br/>
        /// 전체 진행률을 전달받는 객체이며, 진행률 보고가 필요하지 않으면 <see langword="null"/>입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that represents completion of the reload.<br/>
        /// 다시 로드 작업의 완료를 나타내는 비동기 작업입니다.
        /// </returns>
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



        /// <summary>
        /// Gets a handle from the exact registry specified by <paramref name="key"/>.<br/>
        /// <paramref name="key"/>가 지정한 정확한 레지스트리에서 핸들을 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The expected asset type of the handle.<br/>
        /// 핸들이 제공할 것으로 기대하는 에셋 타입입니다.
        /// </typeparam>
        /// <param name="key">
        /// The resource key that identifies the exact registry and asset.<br/>
        /// 정확한 레지스트리와 에셋을 식별하는 리소스 키입니다.
        /// </param>
        /// <returns>
        /// The typed handle, or <see langword="null"/> when the registry or asset is unavailable.<br/>
        /// 타입화된 핸들을 반환하며, 레지스트리나 에셋을 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static IAssetHandle<T>? GetHandle<T>(ResourceKey key) where T : notnull => GetHandle(key) as IAssetHandle<T>;

        /// <summary>
        /// Gets a handle from the exact registry specified by <paramref name="key"/> without priority fallback.<br/>
        /// 우선순위 fallback 없이 <paramref name="key"/>가 지정한 정확한 레지스트리에서 핸들을 가져옵니다.
        /// </summary>
        /// <param name="key">
        /// The resource key that identifies the exact registry and asset.<br/>
        /// 정확한 레지스트리와 에셋을 식별하는 리소스 키입니다.
        /// </param>
        /// <returns>
        /// The handle, or <see langword="null"/> when the registry or asset is unavailable.<br/>
        /// 핸들을 반환하며, 레지스트리나 에셋을 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static IAssetHandle? GetHandle(ResourceKey key) => AssetRegistryManager.Get(key.registryId)?[key.assetId];

        /// <summary>
        /// Gets a handle by searching all registries that provide <typeparamref name="T"/> in descending priority order.<br/>
        /// <typeparamref name="T"/>를 제공하는 모든 레지스트리를 우선순위 내림차순으로 탐색하여 핸들을 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The asset type promised by the compatible registries.<br/>
        /// 호환 레지스트리가 제공한다고 약속한 에셋 타입입니다.
        /// </typeparam>
        /// <param name="identifier">
        /// The asset identifier to search for in each compatible registry.<br/>
        /// 호환 레지스트리에서 검색할 에셋 식별자입니다.
        /// </param>
        /// <returns>
        /// The first matching handle, or <see langword="null"/> when no registry contains the identifier.<br/>
        /// 일치하는 첫 핸들을 반환하며, 어떤 레지스트리에도 식별자가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static IAssetHandle<T>? GetHandle<T>(Identifier identifier) where T : notnull => AssetRegistryManager.GetHandle<T>(identifier);

        /// <summary>
        /// Loads an asset scope by searching compatible registries in descending priority order.<br/>
        /// 호환 레지스트리를 우선순위 내림차순으로 탐색하여 에셋 스코프를 로드합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The asset type of the scope to load.<br/>
        /// 로드할 스코프의 에셋 타입입니다.
        /// </typeparam>
        /// <param name="identifier">
        /// The asset identifier to search for in each compatible registry.<br/>
        /// 호환 레지스트리에서 검색할 에셋 식별자입니다.
        /// </param>
        /// <returns>
        /// The loaded scope, or <see langword="null"/> when no registry contains the identifier.<br/>
        /// 로드된 스코프를 반환하며, 어떤 레지스트리에도 식별자가 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static UniTask<IAssetScope<T>?> LoadScopeAsync<T>(Identifier identifier) where T : notnull => GetHandle<T>(identifier)?.GetScope() ?? UniTask.FromResult<IAssetScope<T>?>(null);

        /// <summary>
        /// Loads an asset scope from the exact registry specified by <paramref name="key"/>.<br/>
        /// <paramref name="key"/>가 지정한 정확한 레지스트리에서 에셋 스코프를 로드합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The asset type of the scope to load.<br/>
        /// 로드할 스코프의 에셋 타입입니다.
        /// </typeparam>
        /// <param name="key">
        /// The resource key that identifies the exact registry and asset.<br/>
        /// 정확한 레지스트리와 에셋을 식별하는 리소스 키입니다.
        /// </param>
        /// <returns>
        /// The loaded scope, or <see langword="null"/> when the registry or asset is unavailable.<br/>
        /// 로드된 스코프를 반환하며, 레지스트리나 에셋을 사용할 수 없으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public static UniTask<IAssetScope<T>?> LoadScopeAsync<T>(ResourceKey key) where T : notnull => GetHandle<T>(key)?.GetScope() ?? UniTask.FromResult<IAssetScope<T>?>(null);
    }
}