#nullable enable
using Cysharp.Threading.Tasks;
using R3;
using RuniOS.IO;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    /// <summary>
    /// 단일 에셋에 대한 참조와 로드/언로드 로직을 관리하는 추상 핸들러입니다.
    /// <br/>에셋의 실제 로드는 <see cref="GetScope"/>를 통해 참조될 때 수행됩니다.
    /// </summary>
    public abstract class AssetHandle<TAsset> : IAssetHandle<TAsset>
    {
        /// <summary>
        /// 에셋 파일에 접근하는 데 사용되는 I/O 핸들러를 가져옵니다.
        /// </summary>
        public IONode node { get; }
        
        /// <summary>
        /// 에셋 파일의 메타 데이터 값을 가져오거나 설정합니다.
        /// </summary>
        public FileMetaData metaData { get; private set; }

        /// <summary>
        /// 에셋 스코프 카운트가 0이 된 후 언로드까지 대기할 프레임 수를 가져옵니다.
        /// </summary>
        public int unloadDelayFrame { get; }
        
        /// <summary>
        /// 로드된 실제 에셋 객체를 가져오거나 설정합니다.
        /// <br/>에셋이 언로드되었거나 아직 로드되지 않은 경우 <see langword="null"/>입니다.
        /// </summary>
        public TAsset? assetObject { get; private set; }
        
        /// <summary>
        /// 에셋이 현재 로드 중인지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool isLoading { get; private set; }

        /// <inheritdoc cref="IAssetHandle.isSealed"/>
        public bool isSealed { get; private set; }
        bool IAssetHandle.isSealed => isSealed;

        // 지연 언로드 감시를 위한 R3 Subject 및 Subscription
        readonly Subject<Unit> _unloadTrigger = new Subject<Unit>();
        IDisposable? _unloadSubscription;
        
        internal readonly List<WeakReference<AssetScope<TAsset>>> assetScopes = new List<WeakReference<AssetScope<TAsset>>>();

        /// <summary>
        /// <see cref="AssetHandle{T}"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="node">에셋 파일에 접근하는 I/O 핸들러입니다.</param>
        /// <param name="metaData">에셋 파일의 초기 메타 데이터입니다.</param>
        /// <param name="unloadDelayFrame">에셋 스코프 카운트가 0이 된 후 언로드까지 대기할 프레임 수입니다. 기본값은 600입니다.</param>
        protected AssetHandle(IONode node, FileMetaData metaData, int unloadDelayFrame = 600)
        {
            this.node = node;
            this.metaData = metaData;

            this.unloadDelayFrame = unloadDelayFrame;
        }

        /// <summary>
        /// 에셋을 비동기적으로 로드하고, 로드된 에셋 객체에 대한 참조를 포함하는 새 <see cref="AssetScope{T}"/>를 생성합니다.
        /// </summary>
        /// <returns>
        /// 로드된 에셋에 대한 <see cref="AssetScope{T}"/> 또는 로드에 실패한 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        public async UniTask<IAssetScope<TAsset>?> GetScope()
        {
            if (isSealed)
            {
                Debug.RuntimeLogWarning($"Cannot create a new AssetScope from sealed AssetHandle '{node.path}'.");
                return null;
            }
            
            // 중복 로딩 방지 (경합 조건 방지)
            while (isLoading)
                await UniTask.Yield();
            
            if (IsDefaultAsset(assetObject) && await node.file.GetEntry() is { } entry)
            {
                isLoading = true;

                try
                {
                    metaData = entry.metaData;
                    assetObject = await Load();
                }
                catch (Exception e)
                {
                    Debug.RuntimeLogError($"Failed to load asset at path {entry.path}! The exception is: {e}");
                    assetObject = GetDefaultAsset();
                }
                finally
                {
                    isLoading = false;
                }
            }

            if (!IsDefaultAsset(assetObject))
            {
                AssetScope<TAsset> scope = new AssetScope<TAsset>(this, assetObject);
                assetScopes.Add(new WeakReference<AssetScope<TAsset>>(scope));

                CancelUnloadWatch();
                return scope;
            }

            return null;
        }

        /// <summary>
        /// 사용이 완료된 <paramref name="assetScope"/>를 반환하고 내부 참조 목록에서 제거합니다.
        /// <br/>스코프 목록이 비어 있고 지연 언로드 프레임이 설정된 경우, 언로드 감시 타이머가 시작됩니다.
        /// </summary>
        /// <param name="assetScope">반환할 에셋 스코프입니다.</param>
        internal void ReturnScope(AssetScope<TAsset> assetScope)
        {
            bool scopeFound = false;
            for (int i = assetScopes.Count - 1; i >= 0; i--)
            {
                WeakReference<AssetScope<TAsset>> weakRef = assetScopes[i];
        
                if (weakRef.TryGetTarget(out AssetScope<TAsset> outAssetScope))
                {
                    // 1. 현재 제거하려는 Scope를 찾았을 경우
                    if (assetScope == outAssetScope)
                    {
                        assetScopes.RemoveAt(i);
                        scopeFound = true;
                        
                        break; 
                    }
                }
                else
                {
                    // 2. WeakReference가 만료되었을 경우 (GC된 경우)
                    // 청소 목적으로 제거
                    assetScopes.RemoveAt(i);
                }
            }

            if (!scopeFound)
            {
                Debug.RuntimeLogWarning
                (
                    $"Invalid or already-returned AssetScope detected! Scope for asset '{node.path}' was not found in the handle's list.\n" +
                    "Possible causes: 1. Scope was returned twice. 2. Scope was disposed outside of its lifecycle."
                );
                
                return;
            }
            
            if (assetScopes.Count <= 0)
            {
                StartUnloadWatch();
                _unloadTrigger.OnNext(Unit.Default);
            }
        }
        
        /// <summary>
        /// 언로드 감시 구독을 시작합니다. <see cref="unloadDelayFrame"/> 동안 추가 참조가 없으면 언로드를 수행합니다.
        /// </summary>
        void StartUnloadWatch()
        {
            // 이미 구독 중이면 무시
            if (_unloadSubscription != null)
                return;
            
            _unloadSubscription = _unloadTrigger
                .DebounceFrame(unloadDelayFrame) 
                .Subscribe(_ => ExecuteUnload());
        }
        
        /// <summary>
        /// 언로드 감시 구독을 취소하고 정리합니다.
        /// </summary>
        void CancelUnloadWatch()
        {
            _unloadSubscription?.Dispose();
            _unloadSubscription = null;
        }

        /// <summary>
        /// 에셋 파일을 비동기적으로 로드합니다.
        /// </summary>
        /// <returns>로드된 에셋 객체 또는 실패 시 <see langword="null"/>을 반환하는 <see cref="TAsset"/>입니다.</returns>
        /// <exception cref="Exception">
        /// 로드 중 발생할 수 있는 모든 예외입니다.
        /// </exception>
        protected abstract UniTask<TAsset> Load();
        
        /// <summary>
        /// 로드된 에셋을 언로드하고 관련된 시스템 리소스를 해제합니다.
        /// </summary>
        /// <exception cref="Exception">
        /// 언로드 중 발생할 수 있는 모든 예외입니다.
        /// </exception>
        protected abstract void Unload();

        internal void ExecuteUnload()
        {
            DisposeQueue.Enqueue(() =>
            {
                try
                {
                    Unload();
                }
                catch (Exception e)
                {
                    Debug.RuntimeLogError($"Failed to unload asset at path {node.path}! The exception is: {e}");
                }

                Debug.Log($"Unloaded asset at path {node.path}");
            });

            assetObject = default;
            CancelUnloadWatch();
        }
        
        protected virtual bool IsDefaultAsset([NotNullWhen(false)] TAsset? asset) => asset == null;
        
        protected virtual TAsset? GetDefaultAsset() => default;

        /// <summary>
        /// 다른 <see cref="IAssetHandle"/>이 현재 핸들과 동일한 에셋을 참조하는지 확인합니다.
        /// <br/>타입, I/O 핸들러, MD5 해시가 모두 일치해야 합니다.
        /// </summary>
        /// <param name="other">비교할 다른 에셋 핸들입니다.</param>
        /// <returns>동일한 에셋을 참조하면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.</returns>
        public virtual bool IsSameTarget(IAssetHandle other)
        {
            if (other is not AssetHandle<TAsset> otherHandle)
                return false;

            return GetType() == other.GetType() && node.IsSameTarget(otherHandle.node) && metaData == otherHandle.metaData;
        }

        /// <inheritdoc/>
        public void Seal() => isSealed = true;
    }
}
