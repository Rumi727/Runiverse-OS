#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in normalize DSP.<br/>
    /// FMOD 내장 노멀라이즈 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class NormalizeDSP : DSP
    {
        NormalizeDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.NORMALIZE;

        public float fadeTime { get => floatParameters[(int)DSP_NORMALIZE.FADETIME]; set => floatParameters[(int)DSP_NORMALIZE.FADETIME] = value; }
        public float threshold { get => floatParameters[(int)DSP_NORMALIZE.THRESHOLD]; set => floatParameters[(int)DSP_NORMALIZE.THRESHOLD] = value; }
        public float maxAmplitude { get => floatParameters[(int)DSP_NORMALIZE.MAXAMP]; set => floatParameters[(int)DSP_NORMALIZE.MAXAMP] = value; }
    }
}
