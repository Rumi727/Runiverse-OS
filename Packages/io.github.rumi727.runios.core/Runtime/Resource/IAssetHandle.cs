#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Resource
{
    public interface IAssetHandle
    {
        /// <summary>
        /// 로드된 실제 에셋 객체를 가져오거나 설정합니다.
        /// <br/>에셋이 언로드되었거나 아직 로드되지 않은 경우 <see langword="null"/>입니다.
        /// </summary>
        object? assetObject { get; }

        /// <summary>
        /// 에셋이 현재 로드 중인지 여부를 가져오거나 설정합니다.
        /// </summary>
        bool isLoading { get; }

        /// <summary>
        /// 핸들이 새 스코프를 반환할 수 없는 상태인지 여부를 가져옵니다.
        /// </summary>
        bool isSealed { get; }
        
        /// <summary>
        /// 에셋을 비동기적으로 로드하고, 로드된 에셋 객체에 대한 참조를 포함하는 새 <see cref="IAssetScope"/>를 생성합니다.
        /// </summary>
        /// <returns>
        /// 로드된 에셋에 대한 <see cref="IAssetScope"/> 또는 로드에 실패한 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        UniTask<IAssetScope?> GetScope();

        /// <summary>
        /// 다른 <see cref="IAssetHandle"/>이 현재 핸들과 동일한 에셋 대상을 참조하는지 확인합니다.<br/>
        /// 핸들 구현에 따라 I/O 대상, 파일 리비전 메타데이터, 사이드카 등의 정보를 비교할 수 있습니다.
        /// </summary>
        /// <param name="other">비교할 다른 에셋 핸들입니다.</param>
        /// <returns>
        /// 동일한 에셋 대상을 참조한다고 판단되면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        bool IsSameTarget(IAssetHandle other);

        /// <summary>
        /// 핸들이 새 스코프를 만들 수 없게 설정합니다.
        /// </summary>
        void Seal();
    }
}