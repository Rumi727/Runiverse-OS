#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in high-pass DSP.<br/>
    /// FMOD 내장 고역통과 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class HighPassDSP : DSP
    {
        HighPassDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.HIGHPASS;

        public float cutoff { get => floatParameters[(int)DSP_HIGHPASS.CUTOFF]; set => floatParameters[(int)DSP_HIGHPASS.CUTOFF] = value; }
        public float resonance { get => floatParameters[(int)DSP_HIGHPASS.RESONANCE]; set => floatParameters[(int)DSP_HIGHPASS.RESONANCE] = value; }
    }
}
