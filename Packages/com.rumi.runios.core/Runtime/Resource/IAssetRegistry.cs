#nullable enable
using Cysharp.Threading.Tasks;
using System.Collections;

namespace RuniOS.Resource
{
    public interface IAssetRegistry : IReadOnlyDictionary<Identifier, IAssetHandle>
    {
        /// <summary>
        /// 이 레지스트리의 고유 id를 나타내는 상수 값입니다.
        /// </summary>
        Identifier registryId { get; }

        /// <summary>
        /// 에셋 타입이 겹칠 때 이 레지스트리를 기본으로 사용할 지 여부를 나타내는 상수 값입니다.
        /// </summary>
        bool isDefault { get; }
        
        Type assetType { get; }
        
        Type handleType { get; }
        
        /// <summary>
        /// 레지스트리의 리소스 로딩 진행 중인지 여부를 가져옵니다.
        /// </summary>
        bool isLoading { get; }
        
        /// <summary>
        /// 에셋 핸들 목록이 변경 사항에 대해 추적 중인지 여부를 가져옵니다.
        /// </summary>
        bool isTracking { get; }

        /// <summary>
        /// 레지스트리에 등록된 모든 에셋 핸들 정보를 지정된 <paramref name="resourcePacks"/>를 기반으로 다시 로드합니다.
        /// </summary>
        /// <param name="resourcePacks">로드에 사용할 리소스 팩 컬렉션입니다.</param>
        /// <param name="progress">작업 진행률을 보고하는 데 사용되는 개체입니다.</param>
        UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}