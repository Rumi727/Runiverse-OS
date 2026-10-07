#nullable enable
namespace RuniOS.Sounds.Processing.Custom
{
    /// <summary>
    /// Base class for custom DSPs that combine two or more inputs into one output.<br/>
    /// 둘 이상의 입력을 출력 하나로 결합하는 Custom DSP의 기반 클래스입니다.
    /// </summary>
    [Obsolete("CustomDSP has not been tested and is quite complex!")]
    public abstract class CustomProcessDSP : CustomDSP
    {
        /// <summary>
        /// Initializes a process DSP before a <see cref="SoundSystem"/> creates its native state.<br/>
        /// <see cref="SoundSystem"/>이 네이티브 상태를 만들기 전 process DSP를 초기화합니다.
        /// </summary>
        protected CustomProcessDSP() { }

        /// <summary>
        /// Gets number of input buffer arrays declared to FMOD.<br/>
        /// FMOD에 선언할 입력 버퍼 배열 수를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Override this value to select input count before <see cref="SoundSystem.CreateDSP(Type)"/> creates native DSP.<br/>
        /// Value must be at least <c>2</c>.<br/><br/>
        /// <see cref="SoundSystem.CreateDSP(Type)"/>가 네이티브 DSP를 생성하기 전에 입력 수를 선택하려면 이 값을 재정의합니다.<br/>
        /// 값은 최소 <c>2</c>여야 합니다.
        /// </remarks>
        public virtual int numInputBuffers => 2;

        /// <summary>
        /// Combines current input buffers into output samples.<br/>
        /// 현재 입력 버퍼를 출력 샘플로 결합합니다.
        /// </summary>
        /// <param name="inputs">
        /// Input buffers, valid only during this callback.<br/>
        /// 이 콜백 동안에만 유효한 입력 버퍼입니다.
        /// </param>
        /// <param name="output">
        /// Output samples, valid only during this callback.<br/>
        /// 이 콜백 동안에만 유효한 출력 샘플입니다.
        /// </param>
        /// <param name="frameCount">
        /// Number of PCM frames in each buffer.<br/>
        /// 각 버퍼의 PCM frame 수입니다.
        /// </param>
        /// <param name="channelCount">
        /// Shared interleaved channel count of all input and output buffers.<br/>
        /// 모든 입력 및 출력 버퍼가 공유하는 인터리브된 채널 수입니다.
        /// </param>
        /// <param name="inputIdle">
        /// Whether FMOD reports idle input.<br/>
        /// FMOD가 입력이 idle 상태라고 보고했는지 여부입니다.
        /// </param>
        protected internal abstract void Process(CustomDSPInputBuffers inputs, Span<float> output, uint frameCount, int channelCount, bool inputIdle);
    }
}
