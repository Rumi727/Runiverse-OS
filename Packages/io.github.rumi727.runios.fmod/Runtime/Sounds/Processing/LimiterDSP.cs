#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in limiter DSP.<br/>
    /// FMOD 내장 리미터 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class LimiterDSP : DSP
    {
        LimiterDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.LIMITER;

        public float releaseTime { get => floatParameters[(int)DSP_LIMITER.RELEASETIME]; set => floatParameters[(int)DSP_LIMITER.RELEASETIME] = value; }
        public float ceiling { get => floatParameters[(int)DSP_LIMITER.CEILING]; set => floatParameters[(int)DSP_LIMITER.CEILING] = value; }
        public float maximizerGain { get => floatParameters[(int)DSP_LIMITER.MAXIMIZERGAIN]; set => floatParameters[(int)DSP_LIMITER.MAXIMIZERGAIN] = value; }
        public bool linked { get => boolParameters[(int)DSP_LIMITER.MODE]; set => boolParameters[(int)DSP_LIMITER.MODE] = value; }
    }
}
