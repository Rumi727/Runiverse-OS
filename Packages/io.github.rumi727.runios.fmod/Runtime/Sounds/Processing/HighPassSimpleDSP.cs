#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in simple high-pass DSP.<br/>
    /// FMOD 내장 단순 고역통과 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class HighPassSimpleDSP : DSP
    {
        HighPassSimpleDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.HIGHPASS_SIMPLE;

        public float cutoff { get => floatParameters[(int)DSP_HIGHPASS_SIMPLE.CUTOFF]; set => floatParameters[(int)DSP_HIGHPASS_SIMPLE.CUTOFF] = value; }
    }
}
