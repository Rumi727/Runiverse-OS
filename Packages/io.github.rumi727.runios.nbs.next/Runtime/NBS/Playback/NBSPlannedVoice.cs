#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Describes a sound that should start for a note, found by an <see cref="NBSTimeline"/> query.<br/>
    /// <see cref="NBSTimeline"/> 조회로 찾은, 노트에 대해 시작해야 하는 소리를 설명합니다.
    /// </summary>
    /// <param name="mapIndex">
    /// The index of the note in <see cref="NBSMap.notes"/>.<br/>
    /// <see cref="NBSMap.notes"/>에서 노트의 인덱스입니다.
    /// </param>
    /// <param name="distance">
    /// The timeline distance in seconds from the query position to the point where the sound starts, along the direction of travel. It can be negative for a late start.<br/>
    /// 조회 위치에서 소리가 시작하는 지점까지 진행 방향 기준의 타임라인 거리(초)입니다. 늦은 시작이면 음수일 수 있습니다.
    /// </param>
    /// <param name="position">
    /// The song time in the coordinates of the note where the sound starts: its entry point, or the wrap point when it enters mid-sound after a loop wrap.<br/>
    /// 노트 좌표 기준으로 소리가 시작하는 곡 시간입니다. 진입점이며, 반복으로 되돌아간 뒤 소리 중간에 들어가면 되돌아간 지점입니다.
    /// </param>
    /// <param name="sourceTime">
    /// The position in the sound, in seconds from its beginning, where playback starts.<br/>
    /// 재생을 시작할 소리 안의 위치이며 소리 시작 기준 초 단위입니다.
    /// </param>
    /// <param name="placement">
    /// The placement of the note.<br/>
    /// 노트의 배치입니다.
    /// </param>
    /// <param name="length">
    /// The timeline length of the sound in seconds after sound stoppers are applied.<br/>
    /// Sound Stopper를 적용한 소리의 타임라인 길이(초)입니다.
    /// </param>
    /// <param name="stopped">
    /// Whether a sound stopper shortens the sound, so playback must end at <see cref="NBSNotePlacement.exit"/>.<br/>
    /// Sound Stopper가 소리를 줄여서 <see cref="NBSNotePlacement.exit"/>에서 재생을 끝내야 하는지 여부입니다.
    /// </param>
    public readonly record struct NBSPlannedVoice(int mapIndex, double distance, double position, double sourceTime, NBSNotePlacement placement, double length, bool stopped);
}
