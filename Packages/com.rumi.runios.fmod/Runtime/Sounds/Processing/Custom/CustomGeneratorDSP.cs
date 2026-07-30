#nullable enable
namespace RuniOS.Sounds.Processing.Custom
{
    /// <summary>
    /// Base class for custom DSPs that generate audio without input.<br/>
    /// 입력 없이 오디오를 생성하는 Custom DSP의 기반 클래스입니다.
    /// </summary>
    [Obsolete("CustomDSP has not been tested and is quite complex!")]
    public abstract class CustomGeneratorDSP : CustomDSP
    {
        /// <summary>
        /// Initializes a generator DSP before a <see cref="SoundSystem"/> creates its native state.<br/>
        /// <see cref="SoundSystem"/>이 네이티브 상태를 만들기 전 generator DSP를 초기화합니다.
        /// </summary>
        protected CustomGeneratorDSP() { }

        /// <summary>
        /// Fills output samples for current DSP callback.<br/>
        /// 현재 DSP 콜백의 출력 샘플을 채웁니다.
        /// </summary>
        /// <param name="output">
        /// Output samples, valid only during this callback.<br/>
        /// 이 콜백 동안에만 유효한 출력 샘플입니다.
        /// </param>
        /// <param name="frameCount">
        /// Number of PCM frames in <paramref name="output"/>.<br/>
        /// <paramref name="output"/>의 PCM frame 수입니다.
        /// </param>
        /// <param name="channelCount">
        /// Interleaved channel count.<br/>
        /// 인터리브된 채널 수입니다.
        /// </param>
        protected internal abstract void Generate(Span<float> output, uint frameCount, int channelCount);
    }
}
