#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in flange DSP.<br/>
    /// FMOD 내장 플랜저 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class FlangeDSP : DSP
    {
        FlangeDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.FLANGE;

        public float mix { get => floatParameters[(int)DSP_FLANGE.MIX]; set => floatParameters[(int)DSP_FLANGE.MIX] = value; }
        public float depth { get => floatParameters[(int)DSP_FLANGE.DEPTH]; set => floatParameters[(int)DSP_FLANGE.DEPTH] = value; }
        public float rate { get => floatParameters[(int)DSP_FLANGE.RATE]; set => floatParameters[(int)DSP_FLANGE.RATE] = value; }
    }
}
