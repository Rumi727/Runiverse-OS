#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in mixer DSP.<br/>
    /// FMOD 내장 믹서 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class MixerDSP : DSP
    {
        MixerDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.MIXER;
    }
}
