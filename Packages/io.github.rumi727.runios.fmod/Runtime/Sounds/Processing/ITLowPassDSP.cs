#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in Impulse Tracker low-pass DSP.<br/>
    /// FMOD 내장 Impulse Tracker 저역통과 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ITLowPassDSP : DSP
    {
        ITLowPassDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.ITLOWPASS;

        public float cutoff { get => floatParameters[(int)DSP_ITLOWPASS.CUTOFF]; set => floatParameters[(int)DSP_ITLOWPASS.CUTOFF] = value; }
        public float resonance { get => floatParameters[(int)DSP_ITLOWPASS.RESONANCE]; set => floatParameters[(int)DSP_ITLOWPASS.RESONANCE] = value; }
    }
}
