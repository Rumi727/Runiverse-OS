#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Selects where a reversed note sound is placed on the timeline when the pitch is negative.<br/>
    /// pitch가 음수일 때 리버스된 노트 소리를 타임라인의 어디에 놓을지 선택합니다.
    /// </summary>
    /// <remarks>
    /// Both modes treat a negative tempo as the time-reversed rendering of the positive tempo with the opposite pitch sign.<br/>
    /// 두 모드 모두 음수 tempo를 pitch 부호가 반대인 양수 tempo 렌더링을 시간축으로 뒤집은 것으로 다룹니다.
    /// </remarks>
    public enum NBSReversePitchAlignment
    {
        /// <summary>
        /// The reversed sound ends with its attack on the note, so the rhythm of the song is kept.<br/>
        /// 리버스된 소리가 노트 위치에서 어택으로 끝나므로 곡의 리듬이 유지됩니다.
        /// </summary>
        attackAtNote,

        /// <summary>
        /// The reversed sound starts on the note, like the reverse mode of a sampler.<br/>
        /// 샘플러의 리버스 모드처럼 리버스된 소리가 노트 위치에서 시작합니다.
        /// </summary>
        startAtNote
    }
}
