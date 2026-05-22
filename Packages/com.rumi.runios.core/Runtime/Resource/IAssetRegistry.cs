#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Resource
{
    public interface IAssetRegistry : IEnumerable<KeyValuePair<Identifier, IAssetHandle>>
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
        /// 식별자로 등록된 특정 에셋을 가져옵니다.<br/>
        /// 등록되지 않았을 경우, null 값입니다.
        /// </summary>
        IAssetHandle? this[Identifier key] { get; }
        
        IEnumerable<Identifier> keys { get; }
        IEnumerable<IAssetHandle> handles { get; }

        int count { get; }

        /// <summary>
        /// 레지스트리에 등록된 모든 에셋 핸들 정보를 지정된 <paramref name="resourcePacks"/>를 기반으로 다시 로드합니다.
        /// </summary>
        /// <param name="resourcePacks">로드에 사용할 리소스 팩 컬렉션입니다.</param>
        /// <param name="progress">작업 진행률을 보고하는 데 사용되는 개체입니다.</param>
        UniTask Reload(IEnumerable<ResourcePack> resourcePacks, IProgress<float>? progress = null);

        bool ContainsKey(Identifier key);
        bool TryGetHandle(Identifier key, [NotNullWhen(true)] out IAssetHandle? handle);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        /// <summary>
        /// 지정된 경로가 레지스트리의 패턴 규칙과 일치하는지 확인합니다.
        /// </summary>
        /// <remarks>
        /// 실제 파일 존재 여부는 확인하지 않으며, 오직 경로 문자열의 형식이 레지스트리가 담당하는 패턴인지 검사합니다.
        /// <br/>경로는 리소스팩 루트를 기준으로 해야 합니다. (예: <c>assets/namespace/textures/...</c>)
        /// </remarks>
        /// <param name="relativePath">검사할 리소스팩 내부 경로입니다.</param>
        /// <returns>패턴이 일치하여 처리 가능한 대상이면 <c>true</c>를 반환합니다.</returns>
        bool IsMatch(RuniPath relativePath);
    }
}