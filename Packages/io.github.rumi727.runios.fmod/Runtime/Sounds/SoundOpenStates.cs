#nullable enable
namespace RuniOS.Sounds
{
    /// <summary>
    /// Stores the current FMOD sound opening and streaming information.<br/>
    /// 현재 FMOD 사운드의 열기 및 스트리밍 정보를 저장합니다.
    /// </summary>
    /// <param name="state">
    /// The current opening or streaming state.<br/>
    /// 현재 열기 또는 스트리밍 상태입니다.
    /// </param>
    /// <param name="bufferedPercent">
    /// The percentage of the stream buffer that is filled.<br/>
    /// 채워진 스트림 버퍼의 백분율입니다.
    /// </param>
    /// <param name="isStarving">
    /// Whether the stream decoder lacks data.<br/>
    /// 스트림 디코더에 데이터가 부족한지 여부입니다.
    /// </param>
    /// <param name="isDiskBusy">
    /// Whether the stream source is busy reading data.<br/>
    /// 스트림 소스가 데이터를 읽느라 사용 중인지 여부입니다.
    /// </param>
    public readonly record struct SoundOpenStates(SoundOpenState state, uint bufferedPercent, bool isStarving, bool isDiskBusy);
}