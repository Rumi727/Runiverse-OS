#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in SFX reverb DSP.<br/>
    /// FMOD 내장 SFX 리버브 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class SFXReverbDSP : DSP
    {
        SFXReverbDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.SFXREVERB;

        public float decayTime { get => floatParameters[(int)DSP_SFXREVERB.DECAYTIME]; set => floatParameters[(int)DSP_SFXREVERB.DECAYTIME] = value; }
        public float earlyDelay { get => floatParameters[(int)DSP_SFXREVERB.EARLYDELAY]; set => floatParameters[(int)DSP_SFXREVERB.EARLYDELAY] = value; }
        public float lateDelay { get => floatParameters[(int)DSP_SFXREVERB.LATEDELAY]; set => floatParameters[(int)DSP_SFXREVERB.LATEDELAY] = value; }
        public float highFrequencyReference { get => floatParameters[(int)DSP_SFXREVERB.HFREFERENCE]; set => floatParameters[(int)DSP_SFXREVERB.HFREFERENCE] = value; }
        public float highFrequencyDecayRatio { get => floatParameters[(int)DSP_SFXREVERB.HFDECAYRATIO]; set => floatParameters[(int)DSP_SFXREVERB.HFDECAYRATIO] = value; }
        public float diffusion { get => floatParameters[(int)DSP_SFXREVERB.DIFFUSION]; set => floatParameters[(int)DSP_SFXREVERB.DIFFUSION] = value; }
        public float density { get => floatParameters[(int)DSP_SFXREVERB.DENSITY]; set => floatParameters[(int)DSP_SFXREVERB.DENSITY] = value; }
        public float lowShelfFrequency { get => floatParameters[(int)DSP_SFXREVERB.LOWSHELFFREQUENCY]; set => floatParameters[(int)DSP_SFXREVERB.LOWSHELFFREQUENCY] = value; }
        public float lowShelfGain { get => floatParameters[(int)DSP_SFXREVERB.LOWSHELFGAIN]; set => floatParameters[(int)DSP_SFXREVERB.LOWSHELFGAIN] = value; }
        public float highCut { get => floatParameters[(int)DSP_SFXREVERB.HIGHCUT]; set => floatParameters[(int)DSP_SFXREVERB.HIGHCUT] = value; }
        public float earlyLateMix { get => floatParameters[(int)DSP_SFXREVERB.EARLYLATEMIX]; set => floatParameters[(int)DSP_SFXREVERB.EARLYLATEMIX] = value; }
        public float wetLevel { get => floatParameters[(int)DSP_SFXREVERB.WETLEVEL]; set => floatParameters[(int)DSP_SFXREVERB.WETLEVEL] = value; }
        public float dryLevel { get => floatParameters[(int)DSP_SFXREVERB.DRYLEVEL]; set => floatParameters[(int)DSP_SFXREVERB.DRYLEVEL] = value; }
    }
}
