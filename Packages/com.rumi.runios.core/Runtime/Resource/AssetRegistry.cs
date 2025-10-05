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
        
        public abstract bool isLoading { get; }

        protected AssetRegistry() => assetHandles = _assetHandles.AsReadOnly();

        /* TODO
         * assetHandles을 다이렉트로 수정하는게 아니라, 변경 사항을 추적하여 변경 사항이 없는 리소스 핸들은 그대로 두고 변경된 리소스 핸들만 교채하는 식으로 재활용 할 것
         */
        public abstract UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);
        
        public async UniTask<IEnumerable<(string nameSpace, IOHandler registryHandler)>> GetRegistryFolder(ResourcePack resourcePack)
        {
            if (await resourcePack.assetFolder.DirectoryExists())
                return (await resourcePack.assetFolder.GetDirectoryHandlers()).Select(x => (x.name, x.CreateChild(registryName)));
            
            return Enumerable.Empty<(string nameSpace, IOHandler registryHandler)>();
        }

        /// <summary>
        /// 내부 에셋 핸들 컬렉션을 비웁니다.
        /// </summary>
        protected void ClearAssetHandle() => _assetHandles.Clear();
        
        /// <summary>
        /// 에셋 핸들을 내부 컬렉션에 추가합니다.
        /// </summary>
        /// <param name="identifier">에셋을 식별하는 고유 ID입니다.</param>
        /// <param name="assetHandle">추가할 에셋 핸들입니다.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="assetHandle"/>의 타입이 내부적으로 요구되는 <c>handleType</c>과 호환되지 않을 때 발생합니다.
        /// </exception>
        protected void AddAssetHandle(Identifier identifier, AssetHandle assetHandle)
        {
            if (!handleType.IsInstanceOfType(assetHandle))
                throw new ArgumentException($"Asset handle of type {assetHandle.GetType().Name} is not compatible with the required type {handleType.Name}.", nameof(assetHandle));
            
            _assetHandles.TryAdd(identifier, assetHandle);
        }
    }
}
