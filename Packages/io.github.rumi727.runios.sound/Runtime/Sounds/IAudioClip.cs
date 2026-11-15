#nullable enable
namespace RuniOS.Sounds
{
    /// <summary>
    /// Represents playable audio data that has a length on the playback timeline.<br/>
    /// 재생 타임라인에서 길이를 가지는 재생 가능한 오디오 데이터를 나타냅니다.
    /// </summary>
    public interface IAudioClip
    {
        /// <summary>
        /// Gets the clip length in seconds.<br/>
        /// 클립 길이를 초 단위로 가져옵니다.
        /// </summary>
        public double length { get; }
    }
}
