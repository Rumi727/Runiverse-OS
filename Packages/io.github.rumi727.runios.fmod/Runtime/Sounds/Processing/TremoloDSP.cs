#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in tremolo DSP.<br/>
    /// FMOD 내장 트레몰로 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class TremoloDSP : DSP
    {
        TremoloDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.TREMOLO;

        public float frequency { get => floatParameters[(int)DSP_TREMOLO.FREQUENCY]; set => floatParameters[(int)DSP_TREMOLO.FREQUENCY] = value; }
        public float depth { get => floatParameters[(int)DSP_TREMOLO.DEPTH]; set => floatParameters[(int)DSP_TREMOLO.DEPTH] = value; }
        public float shape { get => floatParameters[(int)DSP_TREMOLO.SHAPE]; set => floatParameters[(int)DSP_TREMOLO.SHAPE] = value; }
        public float skew { get => floatParameters[(int)DSP_TREMOLO.SKEW]; set => floatParameters[(int)DSP_TREMOLO.SKEW] = value; }
        public float duty { get => floatParameters[(int)DSP_TREMOLO.DUTY]; set => floatParameters[(int)DSP_TREMOLO.DUTY] = value; }
        public float square { get => floatParameters[(int)DSP_TREMOLO.SQUARE]; set => floatParameters[(int)DSP_TREMOLO.SQUARE] = value; }
        public float phase { get => floatParameters[(int)DSP_TREMOLO.PHASE]; set => floatParameters[(int)DSP_TREMOLO.PHASE] = value; }
        public float spread { get => floatParameters[(int)DSP_TREMOLO.SPREAD]; set => floatParameters[(int)DSP_TREMOLO.SPREAD] = value; }
    }
}
