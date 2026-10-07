#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in fader DSP.<br/>
    /// FMOD 내장 페이더 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class FaderDSP : DSP
    {
        FaderDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.FADER;

        public float gain { get => floatParameters[(int)DSP_FADER.GAIN]; set => floatParameters[(int)DSP_FADER.GAIN] = value; }
        public (float linear, float additive) overallGain => GetOverallGainDataParameter((int)DSP_FADER.OVERALL_GAIN);
    }
}
