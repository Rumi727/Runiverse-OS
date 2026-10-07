#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing.Custom
{
    /// <summary>
    /// Base class for custom DSPs that transform one input into one output.<br/>
    /// 입력 하나를 출력 하나로 변환하는 Custom DSP의 기반 클래스입니다.
    /// </summary>
    [Obsolete("CustomDSP has not been tested and is quite complex!")]
    public abstract class CustomReadDSP : CustomDSP
    {
        /// <summary>
        /// Initializes a read DSP before a <see cref="SoundSystem"/> creates its native state.<br/>
        /// <see cref="SoundSystem"/>이 네이티브 상태를 만들기 전 read DSP를 초기화합니다.
        /// </summary>
        protected CustomReadDSP() { }

        /// <summary>
        /// Determines how FMOD should process current input state.<br/>
        /// 현재 입력 상태를 FMOD가 처리할 방식을 결정합니다.
        /// </summary>
        /// <param name="inputIdle">
        /// Whether FMOD reports idle input.<br/>
        /// FMOD가 입력이 idle 상태라고 보고했는지 여부입니다.
        /// </param>
        /// <param name="frameCount">
        /// Number of PCM frames in the input and output buffers.<br/>
        /// 입력과 출력 버퍼의 PCM frame 수입니다.
        /// </param>
        /// <param name="inputMask">
        /// Channel mask reported by FMOD for the input.<br/>
        /// FMOD가 입력에 보고한 channel mask입니다.
        /// </param>
        /// <param name="inputChannels">
        /// Number of input channels.<br/>
        /// 입력 channel 수입니다.
        /// </param>
        /// <param name="speakerMode">
        /// Speaker mode that corresponds to the input format.<br/>
        /// 입력 format에 해당하는 speaker mode입니다.
        /// </param>
        /// <returns>
        /// FMOD result that controls whether <see cref="Read"/> runs.<br/>
        /// <see cref="RESULT.ERR_DSP_DONTPROCESS"/> and <see cref="RESULT.ERR_DSP_SILENCE"/> skip processing.<br/><br/>
        /// <see cref="Read"/> 실행 여부를 제어하는 FMOD 결과입니다.<br/>
        /// <see cref="RESULT.ERR_DSP_DONTPROCESS"/>와 <see cref="RESULT.ERR_DSP_SILENCE"/>는 처리를 건너뜁니다.
        /// </returns>
        protected internal virtual RESULT ShouldProcess(bool inputIdle, uint frameCount, CHANNELMASK inputMask, int inputChannels, SPEAKERMODE speakerMode) => RESULT.OK;

        /// <summary>
        /// Transforms current input samples into output samples.<br/>
        /// 현재 입력 샘플을 출력 샘플로 변환합니다.
        /// </summary>
        /// <param name="input">
        /// Input samples, valid only during this callback.<br/>
        /// 이 콜백 동안에만 유효한 입력 샘플입니다.
        /// </param>
        /// <param name="output">
        /// Output samples, valid only during this callback.<br/>
        /// 이 콜백 동안에만 유효한 출력 샘플입니다.
        /// </param>
        /// <param name="frameCount">
        /// Number of PCM frames in both spans.<br/>
        /// 두 Span의 PCM frame 수입니다.
        /// </param>
        /// <param name="channelCount">
        /// Interleaved channel count.<br/>
        /// 인터리브된 채널 수입니다.
        /// </param>
        protected internal abstract void Read(ReadOnlySpan<float> input, Span<float> output, uint frameCount, int channelCount);
    }
}
