#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in three-band equalizer DSP.<br/>
    /// FMOD 내장 3밴드 이퀄라이저 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ThreeEQDSP : DSP
    {
        ThreeEQDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.THREE_EQ;

        public float lowGain { get => floatParameters[(int)DSP_THREE_EQ.LOWGAIN]; set => floatParameters[(int)DSP_THREE_EQ.LOWGAIN] = value; }
        public float midGain { get => floatParameters[(int)DSP_THREE_EQ.MIDGAIN]; set => floatParameters[(int)DSP_THREE_EQ.MIDGAIN] = value; }
        public float highGain { get => floatParameters[(int)DSP_THREE_EQ.HIGHGAIN]; set => floatParameters[(int)DSP_THREE_EQ.HIGHGAIN] = value; }
        public float lowCrossover { get => floatParameters[(int)DSP_THREE_EQ.LOWCROSSOVER]; set => floatParameters[(int)DSP_THREE_EQ.LOWCROSSOVER] = value; }
        public float highCrossover { get => floatParameters[(int)DSP_THREE_EQ.HIGHCROSSOVER]; set => floatParameters[(int)DSP_THREE_EQ.HIGHCROSSOVER] = value; }
        public ThreeEQCrossoverSlope crossoverSlope { get => (ThreeEQCrossoverSlope)intParameters[(int)DSP_THREE_EQ.CROSSOVERSLOPE]; set => intParameters[(int)DSP_THREE_EQ.CROSSOVERSLOPE] = (int)value; }
    }
}
