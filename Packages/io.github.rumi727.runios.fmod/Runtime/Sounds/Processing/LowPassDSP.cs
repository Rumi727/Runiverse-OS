#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in low-pass DSP.<br/>
    /// FMOD 내장 저역통과 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class LowPassDSP : DSP
    {
        internal LowPassDSP() { }

        internal override DSP_TYPE type => DSP_TYPE.LOWPASS;

        /// <summary>
        /// Gets or sets cutoff frequency in hertz.<br/>
        /// 헤르츠 단위 컷오프 주파수를 가져오거나 설정합니다.
        /// </summary>
        public float cutoff
        {
            get => floatParameters[(int)DSP_LOWPASS.CUTOFF];
            set => floatParameters[(int)DSP_LOWPASS.CUTOFF] = value;
        }

        /// <summary>
        /// Gets or sets filter resonance.<br/>
        /// 필터 공진 값을 가져오거나 설정합니다.
        /// </summary>
        public float resonance
        {
            get => floatParameters[(int)DSP_LOWPASS.RESONANCE];
            set => floatParameters[(int)DSP_LOWPASS.RESONANCE] = value;
        }
    }
}
