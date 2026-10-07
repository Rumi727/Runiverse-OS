#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in parametric equalizer DSP.<br/>
    /// FMOD 내장 파라메트릭 이퀄라이저 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ParamEQDSP : DSP
    {
        ParamEQDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.PARAMEQ;

        public float center { get => floatParameters[(int)DSP_PARAMEQ.CENTER]; set => floatParameters[(int)DSP_PARAMEQ.CENTER] = value; }
        public float bandwidth { get => floatParameters[(int)DSP_PARAMEQ.BANDWIDTH]; set => floatParameters[(int)DSP_PARAMEQ.BANDWIDTH] = value; }
        public float gain { get => floatParameters[(int)DSP_PARAMEQ.GAIN]; set => floatParameters[(int)DSP_PARAMEQ.GAIN] = value; }
    }
}
