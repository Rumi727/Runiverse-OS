#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.Resource
{
    public abstract class AssetRegistry
    {
        public abstract string registryName { get; }
        
        public abstract Type handleType { get; }
        public abstract Type scopeType { get; }

        readonly Dictionary<Identifier, AssetHandle> _assetHandles = new();
        public IReadOnlyDictionary<Identifier, AssetHandle> assetHandles { get; }

        readonly HashSet<Identifier> trackedIdentifier = new();
        
        public abstract bool isLoading { get; }
        
        /// <summary>
        /// 에셋 핸들 목록이 추적되고 있는지 여부입니다.
        /// </summary>
        public bool isTracking { get; private set; }

        protected AssetRegistry() => assetHandles = _assetHandles.AsReadOnly();
        
        /// <summary>
        /// 에셋 핸들 컬렉션 변경 사항에 대한 트래킹을 시작합니다.
        /// <br/>이 메서드가 호출된 후부터 <see cref="RecordAssetHandle(Identifier, AssetHandle)"/>을 호출할 수 있습니다.
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
        /// <br/>이번 트래킹 세션에서 <see cref="RecordAssetHandle(Identifier, AssetHandle)"/>을 통해 등록되지 않은
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
                _assetHandles.Remove(item);
            
            trackedIdentifier.Clear();
            isTracking = false;
        }
        
        public abstract UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);
        
        public async UniTask<IEnumerable<(string nameSpace, IOHandler registryHandler)>> GetRegistryFolder(ResourcePack resourcePack)
        {
            if (await resourcePack.assetFolder.DirectoryExists())
                return (await resourcePack.assetFolder.GetDirectoryHandlers()).Select(x => (x.name, x.CreateChild(registryName)));
            
            return Enumerable.Empty<(string nameSpace, IOHandler registryHandler)>();
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
        protected void RecordAssetHandle(Identifier identifier, AssetHandle assetHandle)
        {
            if (!isTracking)
                throw new InvalidOperationException("Tracking is not started. Call BeginTracking() before adding asset handles.");
            if (!handleType.IsInstanceOfType(assetHandle))
                throw new ArgumentException($"Asset handle of type {assetHandle.GetType().Name} is not compatible with the required type {handleType.Name}.", nameof(assetHandle));

            // identifier가 이미 트래킹되고 있다면 중복 등록 방지 (Tracking)
            if (!trackedIdentifier.Add(identifier))
                return;
            
            // 핸들이 없거나 IOHandler가 다를 경우에만 교체 (Register/Update)
            if (!_assetHandles.TryGetValue(identifier, out AssetHandle? value) || !value.ioHandler.IsSameTarget(assetHandle.ioHandler))
                _assetHandles[identifier] = assetHandle;
        }
    }
}
