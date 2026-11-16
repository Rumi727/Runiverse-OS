#nullable enable
// ReSharper disable Unity.RedundantAttributeOnTarget
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores the properties of one NBS layer row.<br/>
    /// NBS 레이어 행 하나의 속성을 저장합니다.
    /// </summary>
    /// <param name="name">
    /// The layer name.<br/>
    /// 레이어 이름입니다.
    /// </param>
    /// <param name="lockState">
    /// The stored lock state.<br/>
    /// 저장된 잠금 상태입니다.
    /// </param>
    /// <param name="volume">
    /// The layer volume in percent.<br/>
    /// 퍼센트 단위의 레이어 음량입니다.
    /// </param>
    /// <param name="panning">
    /// The layer stereo position where <c>100</c> is the center.<br/>
    /// <c>100</c>이 중앙인 레이어 스테레오 위치입니다.
    /// </param>
    [Serializable]
    public record struct NBSLayer
    (
        [field: SerializeField] string name,
        [field: SerializeField] NBSLayerLock lockState,
        [field: SerializeField] byte volume,
        [field: SerializeField] byte panning
    );
}
