#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    /// <summary>
    /// 특정 타입의 에셋 핸들을 관리하고 로드 로직을 정의하는 추상 클래스입니다.
    /// </summary>
    public abstract class AssetRegistry<THandle> : IAssetRegistry<THandle> where THandle : class, IAssetHandle
    {
        /// <summary>
        /// 이 레지스트리의 고유 id를 나타내는 상수 값입니다.
        /// </summary>
        public abstract Identifier registryId { get; }

        /// <summary>
        /// 에셋 타입이 겹칠 때 이 레지스트리를 기본으로 사용할 지 여부를 나타내는 상수 값입니다.
        /// </summary>
        public virtual bool isDefault => false;
        
        public abstract Type assetType { get; }

        /// <summary>
        /// 레지스트리의 리소스 로딩 진행 중인지 여부를 가져옵니다.
        /// </summary>
        public abstract bool isLoading { get; }
        
        /// <summary>
        /// 에셋 핸들 목록이 변경 사항에 대해 추적 중인지 여부를 가져옵니다.
        /// </summary>
        public bool isTracking { get; private set; }
        
        public THandle this[Identifier key] => assetHandles[key];
        
        public IEnumerable<Identifier> Keys => assetHandles.Keys;
        public IEnumerable<THandle> Values => assetHandles.Values;
        
        public int Count => assetHandles.Count;
        
        readonly Dictionary<Identifier, THandle> assetHandles = new();
        readonly HashSet<Identifier> trackedIdentifier = new();
        
        /// <summary>
        /// 에셋 핸들 컬렉션 변경 사항에 대한 트래킹을 시작합니다.
        /// <br/>이 메서드가 호출된 후부터 <see cref="RecordAssetHandle(Identifier, THandle)"/>을 호출할 수 있습니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 트래킹이 이미 시작된 상태(<see cref="isTracking"/>이 <see langword="true"/>)일 때 발생합니다.
        /// </exception>
        protected void BeginTracking()
        {
            if (isTracking)
                throw new InvalidOperationException("Tracking is already started.");
            
            isTracking = true;
            trackedIdentifier.Clear();
        }
        
        /// <summary>
        /// 에셋 핸들 컬렉션 변경 사항에 대한 트래킹을 종료하고,
        /// <br/>이번 트래킹 세션에서 <see cref="RecordAssetHandle(Identifier, THandle)"/>을 통해 등록되지 않은
        /// <br/>기존 에셋 핸들을 컬렉션에서 제거합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// 트래킹이 시작되지 않은 상태(<see cref="isTracking"/>이 <see langword="false"/>)일 때 발생합니다.
        /// </exception>
        protected void EndTracking()
        {
            if (!isTracking)
                throw new InvalidOperationException("Tracking is not started. Cannot end tracking.");

            var keysToRemove = assetHandles.Keys.Where(x => !trackedIdentifier.Contains(x)).ToArray();
            foreach (var item in keysToRemove)
                assetHandles.Remove(item);

            trackedIdentifier.Clear();
            isTracking = false;
        }
        
        /// <summary>
        /// 에셋 핸들을 내부 컬렉션에 등록하고 해당 <paramref name="identifier"/>를 트래킹 목록에 추가합니다.
        /// <br/>핸들 등록 및 트래킹은 트래킹이 시작된 상태에서만 유효합니다.
        /// </summary>
        /// <param name="identifier">에셋을 식별하는 고유 ID입니다.</param>
        /// <param name="assetHandle">등록할 에셋 핸들입니다.</param>
        /// <exception cref="InvalidOperationException">
        /// <see cref="BeginTracking"/> 메소드가 호출되지 않은 상태일 때 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="assetHandle"/>의 타입이 내부적으로 요구되는 <c>handleType</c>과 호환되지 않을 때 발생합니다.
        /// </exception>
        protected void RecordAssetHandle(Identifier identifier, THandle assetHandle)
        {
            if (!isTracking)
                throw new InvalidOperationException("Tracking is not started. Call BeginTracking() before adding asset handles.");

            // identifier가 이미 트래킹되고 있다면 중복 등록 방지 (Tracking)
            if (!trackedIdentifier.Add(identifier))
                return;
            
            // 핸들이 없거나 IOHandler가 다를 경우에만 교체 (Register/Update)
            if (!assetHandles.TryGetValue(identifier, out THandle? value) || !value.IsSameTarget(assetHandle))
                assetHandles[identifier] = assetHandle;
        }
        
        /// <summary>
        /// 레지스트리에 등록된 모든 에셋 핸들 정보를 지정된 <paramref name="resourcePacks"/>를 기반으로 다시 로드합니다.
        /// </summary>
        /// <param name="resourcePacks">로드에 사용할 리소스 팩 컬렉션입니다.</param>
        /// <param name="progress">작업 진행률을 보고하는 데 사용되는 개체입니다.</param>
        public abstract UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);
        
        public bool ContainsKey(Identifier key) => assetHandles.ContainsKey(key);
        
        public bool TryGetValue(Identifier key, out THandle value) => assetHandles.TryGetValue(key, out value);
        
        public IEnumerator<KeyValuePair<Identifier, THandle>> GetEnumerator() => assetHandles.GetEnumerator();
    }
}