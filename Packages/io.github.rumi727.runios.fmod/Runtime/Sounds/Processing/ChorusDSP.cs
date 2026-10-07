#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in chorus DSP.<br/>
    /// FMOD 내장 코러스 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ChorusDSP : DSP
    {
        ChorusDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.CHORUS;

        public float mix { get => floatParameters[(int)DSP_CHORUS.MIX]; set => floatParameters[(int)DSP_CHORUS.MIX] = value; }
        public float rate { get => floatParameters[(int)DSP_CHORUS.RATE]; set => floatParameters[(int)DSP_CHORUS.RATE] = value; }
        public float depth { get => floatParameters[(int)DSP_CHORUS.DEPTH]; set => floatParameters[(int)DSP_CHORUS.DEPTH] = value; }
    }
}
