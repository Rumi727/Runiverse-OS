#nullable enable
// ReSharper disable Unity.RedundantAttributeOnTarget
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores one note block placed at a tick and layer coordinate.<br/>
    /// tick과 레이어 좌표에 놓인 노트 블록 하나를 저장합니다.
    /// </summary>
    /// <param name="tick">
    /// The zero-based tick that contains the note.<br/>
    /// 노트가 들어 있는 0부터 시작하는 tick입니다.
    /// </param>
    /// <param name="layer">
    /// The zero-based layer that contains the note. It may exceed the number of layer definitions.<br/>
    /// 노트가 들어 있는 0부터 시작하는 레이어입니다. 레이어 정의 수를 넘을 수 있습니다.
    /// </param>
    /// <param name="instrument">
    /// The instrument index in the current layout of <see cref="NBSInstrument.vanillaCount"/> built-in instruments followed by custom instruments.<br/>
    /// <see cref="NBSInstrument.vanillaCount"/>개의 기본 악기 뒤에 커스텀 악기가 이어지는 현재 배치 기준의 악기 인덱스입니다.
    /// </param>
    /// <param name="key">
    /// The key from <c>0</c> (A0) to <c>87</c> (C8). <c>45</c> is F#4.<br/>
    /// <c>0</c>(A0)부터 <c>87</c>(C8)까지의 건반입니다. <c>45</c>는 F#4입니다.
    /// </param>
    /// <param name="velocity">
    /// The note volume in percent.<br/>
    /// 퍼센트 단위의 노트 음량입니다.
    /// </param>
    /// <param name="panning">
    /// The stereo position where <c>100</c> is the center.<br/>
    /// <c>100</c>이 중앙인 스테레오 위치입니다.
    /// </param>
    /// <param name="pitch">
    /// The fine pitch in cents.<br/>
    /// 센트 단위의 미세 피치입니다.
    /// </param>
    [Serializable]
    public record struct NBSNote
    (
        [field: SerializeField] int tick,
        [field: SerializeField] int layer,
        [field: SerializeField] int instrument,
        [field: SerializeField] byte key,
        [field: SerializeField] byte velocity,
        [field: SerializeField] byte panning,
        [field: SerializeField] short pitch
    );
}
