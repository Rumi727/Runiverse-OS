#nullable enable
using Cysharp.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    /// <summary>
    /// Provides storage and reload behavior for asset handles of a specific type.<br/>
    /// 특정 타입의 에셋 핸들을 저장하고 다시 로드하는 동작을 제공합니다.
    /// </summary>
    /// <typeparam name="THandle">
    /// The type of handle stored by the registry.<br/>
    /// 레지스트리가 저장하는 핸들의 타입입니다.
    /// </typeparam>
    public abstract class AssetRegistry<THandle> : IAssetRegistry<THandle> where THandle : IAssetHandle
    {
        /// <summary>
        /// Gets the unique identifier of this registry.<br/>
        /// 이 레지스트리의 고유 식별자를 가져옵니다.
        /// </summary>
        public abstract Identifier registryId { get; }

        /// <summary>
        /// Gets the priority used to order registries that provide the same <see cref="providedAssetType"/> during <see cref="AssetRefMode.automatic"/> resolution.<br/>
        /// <see cref="AssetRefMode.automatic"/> 확인 과정에서 동일한 <see cref="providedAssetType"/>을 제공하는 레지스트리의 순서를 정하는 우선순위를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Registries with higher values are searched first. The value must remain unchanged after registration.<br/>
        /// 값이 클수록 먼저 탐색하며, 레지스트리 등록 후에는 값을 변경하지 않아야 합니다.
        /// <br/><br/>
        /// The relative order of registries with equal priorities is unspecified.<br/>
        /// 동일한 우선순위 레지스트리 사이의 상대적인 순서는 지정되지 않습니다.
        /// </remarks>
        public virtual int priority => 0;

        /// <summary>
        /// Gets the asset type this registry promises to provide through its handles.<br/>
        /// 이 레지스트리가 핸들을 통해 제공한다고 약속하는 에셋 타입을 가져옵니다.
        /// </summary>
        public abstract Type providedAssetType { get; }

        /// <summary>
        /// Gets whether this registry is currently loading resources.<br/>
        /// 이 레지스트리가 현재 리소스를 로드하고 있는지 여부를 가져옵니다.
        /// </summary>
        public abstract bool isLoading { get; }
        
        /// <summary>
        /// Gets whether this registry is currently tracking asset handle changes.<br/>
        /// 이 레지스트리가 현재 에셋 핸들 변경 사항을 추적하고 있는지 여부를 가져옵니다.
        /// </summary>
        [MemberNotNullWhen(true, nameof(trackedHandles))]
        public bool isTracking => trackedHandles != null;

        /// <inheritdoc/>
        public THandle? this[Identifier key]
        {
            get
            {
                if (TryGetHandle(key, out var handle))
                    return handle;

                return default;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<Identifier> keys => assetHandles.Keys;

        /// <inheritdoc/>
        public IEnumerable<THandle> handles => assetHandles.Values;

        /// <inheritdoc/>
        public int count => assetHandles.Count;

        Dictionary<Identifier, THandle> assetHandles = new();
        Dictionary<Identifier, THandle>? trackedHandles = null;
        
        /// <summary>
        /// Starts tracking changes to the asset handle collection.<br/>
        /// 에셋 핸들 컬렉션의 변경 사항을 추적하기 시작합니다.
        /// <br/><br/>
        /// After this method is called, derived classes can call <see cref="RecordAssetHandle(Identifier, THandle)"/>.<br/>
        /// 이 메서드를 호출한 후 파생 클래스는 <see cref="RecordAssetHandle(Identifier, THandle)"/>을 호출할 수 있습니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when tracking has already started, as indicated by <see cref="isTracking"/> being <see langword="true"/>.<br/>
        /// <see cref="isTracking"/>이 <see langword="true"/>인 이미 추적 중인 상태에서 호출하면 발생합니다.
        /// </exception>
        protected void BeginTracking()
        {
            if (isTracking)
                throw new InvalidOperationException("Tracking is already started.");

            trackedHandles = new Dictionary<Identifier, THandle>();
        }
        
        /// <summary>
        /// Stops tracking asset handle changes and removes existing handles that were not recorded during the session.<br/>
        /// 에셋 핸들 변경 사항의 추적을 종료하고, 해당 세션에서 기록되지 않은 기존 핸들을 제거합니다.
        /// <br/><br/>
        /// Handles are recorded through <see cref="RecordAssetHandle(Identifier, THandle)"/>.<br/>
        /// 핸들은 <see cref="RecordAssetHandle(Identifier, THandle)"/>을 통해 기록합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when tracking has not started, as indicated by <see cref="isTracking"/> being <see langword="false"/>.<br/>
        /// <see cref="isTracking"/>이 <see langword="false"/>인 추적하지 않는 상태에서 호출하면 발생합니다.
        /// </exception>
        protected void EndTracking()
        {
            if (!isTracking)
                throw new InvalidOperationException("Tracking is not started. Cannot end tracking.");

            assetHandles = trackedHandles;
            trackedHandles = null;
        }

        /// <summary>
        /// Determines whether the specified <paramref name="identifier"/> is currently being tracked.<br/>
        /// 지정된 <paramref name="identifier"/>가 현재 추적되고 있는지 확인합니다.
        /// </summary>
        /// <param name="identifier">
        /// The asset identifier to check.<br/>
        /// 확인할 에셋 식별자입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the identifier is tracked; otherwise, <see langword="false"/>.<br/>
        /// 식별자가 추적 중이면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="BeginTracking"/> has not been called.<br/>
        /// <see cref="BeginTracking"/>을 호출하지 않은 상태에서 발생합니다.
        /// </exception>
        protected bool IsTracked(Identifier identifier)
        {
            if (!isTracking)
                throw new InvalidOperationException("Tracking is not started. Call BeginTracking() before adding asset handles.");
            
            return trackedHandles.ContainsKey(identifier);
        }

        /// <summary>
        /// Records an asset handle in the tracked collection for the specified identifier.<br/>
        /// 지정된 식별자에 대한 에셋 핸들을 추적 중인 컬렉션에 기록합니다.
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier of the asset.<br/>
        /// 에셋의 고유 식별자입니다.
        /// </param>
        /// <param name="assetHandle">
        /// The handle to record.<br/>
        /// 기록할 핸들입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <see cref="BeginTracking"/> has not been called.<br/>
        /// <see cref="BeginTracking"/>을 호출하지 않은 상태에서 발생합니다.
        /// </exception>
        protected void RecordAssetHandle(Identifier identifier, THandle assetHandle)
        {
            if (!isTracking)
                throw new InvalidOperationException("Tracking is not started. Call BeginTracking() before adding asset handles.");

            /*
             * TODO
             * 따로 중복 체크를 하는 API를 만들어서 의미 없는 에셋 핸들을 만들게하지 말 것
             */

            // identifier가 이미 트래킹되고 있다면 중복 등록 방지 (Tracking)
            if (IsTracked(identifier))
                return;

            // 핸들이 없거나 IOHandler가 다를 경우에만 교체 (Register/Update)
            if (assetHandles.TryGetValue(identifier, out THandle? value) && value.IsSameTarget(assetHandle))
                assetHandle = value;
            
            trackedHandles[identifier] = assetHandle;
        }
        
        /// <summary>
        /// Reloads all asset handles registered by this registry from the specified resource packs.<br/>
        /// 지정된 리소스 팩을 기반으로 이 레지스트리에 등록된 모든 에셋 핸들을 다시 로드합니다.
        /// </summary>
        /// <param name="resourcePacks">
        /// The resource packs used for loading.<br/>
        /// 로드에 사용할 리소스 팩 컬렉션입니다.
        /// </param>
        /// <param name="progress">
        /// The object that receives progress updates, or <see langword="null"/> when progress reporting is not required.<br/>
        /// 작업 진행률을 전달받는 객체이며, 진행률 보고가 필요하지 않으면 <see langword="null"/>입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that represents completion of the reload.<br/>
        /// 다시 로드 작업의 완료를 나타내는 비동기 작업입니다.
        /// </returns>
        public abstract UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);

        /// <inheritdoc/>
        public bool ContainsKey(Identifier key) => assetHandles.ContainsKey(key);

        /// <inheritdoc/>
        public bool TryGetHandle(Identifier key, [NotNullWhen(true)] out THandle? handle) => assetHandles.TryGetValue(key, out handle);

        /// <summary>
        /// Returns an enumerator for the registered asset handles.<br/>
        /// 등록된 에셋 핸들을 열거하는 열거자를 반환합니다.
        /// </summary>
        public IEnumerator<KeyValuePair<Identifier, THandle>> GetEnumerator() => assetHandles.GetEnumerator();
    }
}