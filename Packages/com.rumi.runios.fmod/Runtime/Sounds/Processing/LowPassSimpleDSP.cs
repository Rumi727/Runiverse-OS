#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in simple low-pass DSP.<br/>
    /// FMOD 내장 단순 저역통과 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class LowPassSimpleDSP : DSP
    {
        LowPassSimpleDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.LOWPASS_SIMPLE;

        public float cutoff { get => floatParameters[(int)DSP_LOWPASS_SIMPLE.CUTOFF]; set => floatParameters[(int)DSP_LOWPASS_SIMPLE.CUTOFF] = value; }
    }
}
