#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in compressor DSP.<br/>
    /// FMOD 내장 컴프레서 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class CompressorDSP : DSP
    {
        CompressorDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.COMPRESSOR;

        public float threshold { get => floatParameters[(int)DSP_COMPRESSOR.THRESHOLD]; set => floatParameters[(int)DSP_COMPRESSOR.THRESHOLD] = value; }
        public float ratio { get => floatParameters[(int)DSP_COMPRESSOR.RATIO]; set => floatParameters[(int)DSP_COMPRESSOR.RATIO] = value; }
        public float attack { get => floatParameters[(int)DSP_COMPRESSOR.ATTACK]; set => floatParameters[(int)DSP_COMPRESSOR.ATTACK] = value; }
        public float release { get => floatParameters[(int)DSP_COMPRESSOR.RELEASE]; set => floatParameters[(int)DSP_COMPRESSOR.RELEASE] = value; }
        public float makeupGain { get => floatParameters[(int)DSP_COMPRESSOR.GAINMAKEUP]; set => floatParameters[(int)DSP_COMPRESSOR.GAINMAKEUP] = value; }
        public bool useSidechain { get => GetBooleanDataParameter((int)DSP_COMPRESSOR.USESIDECHAIN); set => SetBooleanDataParameter((int)DSP_COMPRESSOR.USESIDECHAIN, value); }
        public bool linked { get => boolParameters[(int)DSP_COMPRESSOR.LINKED]; set => boolParameters[(int)DSP_COMPRESSOR.LINKED] = value; }
    }
}
