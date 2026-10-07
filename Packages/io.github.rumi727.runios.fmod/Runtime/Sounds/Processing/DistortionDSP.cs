#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in distortion DSP.<br/>
    /// FMOD 내장 디스토션 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class DistortionDSP : DSP
    {
        DistortionDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.DISTORTION;

        public float level { get => floatParameters[(int)DSP_DISTORTION.LEVEL]; set => floatParameters[(int)DSP_DISTORTION.LEVEL] = value; }
    }
}
