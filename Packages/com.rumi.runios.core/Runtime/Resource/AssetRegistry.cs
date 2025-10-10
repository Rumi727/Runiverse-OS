#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RuniOS.Resource
{
    /// <summary>
    /// 특정 타입의 에셋 핸들을 관리하고 로드 로직을 정의하는 추상 클래스입니다.
    /// </summary>
    public abstract class AssetRegistry
    {
        /// <summary>
        /// 이 레지스트리가 관리하는 파일 구조 내 폴더의 이름을 가져옵니다.
        /// </summary>
        public abstract string registryName { get; }
        
        /// <summary>
        /// 이 레지스트리가 관리하는 에셋 핸들의 구체적인 타입을 가져옵니다.
        /// </summary>
        public abstract Type handleType { get; }
        
        /// <summary>
        /// 이 레지스트리가 관리하는 에셋 스코프의 구체적인 타입을 가져옵니다.
        /// </summary>
        public abstract Type scopeType { get; }
        
        /// <summary>
        /// 이 레지스트리가 현재 추적하고 있는 모든 에셋 핸들을 가져옵니다.
        /// </summary>
        public IReadOnlyDictionary<Identifier, AssetHandle> assetHandles { get; }
        readonly Dictionary<Identifier, AssetHandle> _assetHandles = new();

        readonly HashSet<Identifier> trackedIdentifier = new();
        
        /// <summary>
        /// 레지스트리의 리소스 로딩 진행 중인지 여부를 가져옵니다.
        /// </summary>
        public abstract bool isLoading { get; }
        
        /// <summary>
        /// 에셋 핸들 목록이 변경 사항에 대해 추적 중인지 여부를 가져옵니다.
        /// </summary>
        public bool isTracking { get; private set; }

        /// <summary>
        /// <see cref="AssetRegistry"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
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
        
        /// <summary>
        /// 레지스트리에 등록된 모든 에셋 핸들 정보를 지정된 <paramref name="resourcePacks"/>를 기반으로 다시 로드합니다.
        /// </summary>
        /// <param name="resourcePacks">로드에 사용할 리소스 팩 컬렉션입니다.</param>
        /// <param name="progress">작업 진행률을 보고하는 데 사용되는 개체입니다.</param>
        public abstract UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);
        
        /// <summary>
        /// 지정된 <paramref name="resourcePack"/> 내에서 이 레지스트리의 데이터를 포함하는 폴더를 비동기적으로 열거합니다.
        /// <br/>각 폴더는 네임스페이스와 해당 레지스트리 핸들러를 반환합니다.
        /// </summary>
        /// <param name="resourcePack">검색할 리소스 팩입니다.</param>
        /// <returns>비동기적으로 네임스페이스 이름과 레지스트리 핸들러를 반환하는 열거자입니다.</returns>
        public async IAsyncEnumerable<(string nameSpace, IOHandler registryHandler)> GetRegistryFolder(ResourcePack resourcePack)
        { 
            if (!await resourcePack.assetFolder.DirectoryExists())
                yield break;

            foreach (var nameSpaceHandler in await resourcePack.assetFolder.GetDirectoryHandlers())
            {
                IOHandler registryHandler = nameSpaceHandler.CreateChild(registryName);
                if (!await registryHandler.DirectoryExists())
                    continue;
                
                yield return (nameSpaceHandler.name, registryHandler);
            }
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
            if (!_assetHandles.TryGetValue(identifier, out AssetHandle? value) || !value.IsSameTarget(assetHandle))
                _assetHandles[identifier] = assetHandle;
        }
    }
}
