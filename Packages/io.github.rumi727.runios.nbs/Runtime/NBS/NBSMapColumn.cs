#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Stores one tick column that contains at least one note.<br/>
    /// 노트가 하나 이상 있는 tick 열 하나를 저장합니다.
    /// </summary>
    /// <param name="tick">
    /// The tick of the column.<br/>
    /// 열의 tick입니다.
    /// </param>
    /// <param name="time">
    /// The song time of the column in seconds.<br/>
    /// 열의 곡 시간(초)입니다.
    /// </param>
    /// <param name="noteStart">
    /// The index of the first note of the column in <see cref="NBSMap.notes"/>.<br/>
    /// <see cref="NBSMap.notes"/>에서 열의 첫 노트 인덱스입니다.
    /// </param>
    /// <param name="noteCount">
    /// The number of notes in the column. Notes are in ascending layer order.<br/>
    /// 열에 있는 노트 수입니다. 노트는 레이어 오름차순입니다.
    /// </param>
    public readonly record struct NBSMapColumn(int tick, double time, int noteStart, int noteCount);
}
