#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in Impulse Tracker echo DSP.<br/>
    /// FMOD 내장 Impulse Tracker 에코 DSP를 안전하게 소유합니다.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public sealed class ITEchoDSP : DSP
    {
        ITEchoDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.ITECHO;

        public float wetDryBalance { get => floatParameters[(int)DSP_ITECHO.WETDRYMIX]; set => floatParameters[(int)DSP_ITECHO.WETDRYMIX] = value; }
        public float feedback { get => floatParameters[(int)DSP_ITECHO.FEEDBACK]; set => floatParameters[(int)DSP_ITECHO.FEEDBACK] = value; }
        public float leftDelay { get => floatParameters[(int)DSP_ITECHO.LEFTDELAY]; set => floatParameters[(int)DSP_ITECHO.LEFTDELAY] = value; }
        public float rightDelay { get => floatParameters[(int)DSP_ITECHO.RIGHTDELAY]; set => floatParameters[(int)DSP_ITECHO.RIGHTDELAY] = value; }
        public float panDelay { get => floatParameters[(int)DSP_ITECHO.PANDELAY]; set => floatParameters[(int)DSP_ITECHO.PANDELAY] = value; }
    }
}
