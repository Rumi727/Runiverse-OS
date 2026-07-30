#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in echo DSP.<br/>
    /// FMOD 내장 에코 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class EchoDSP : DSP
    {
        EchoDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.ECHO;

        public float delay { get => floatParameters[(int)DSP_ECHO.DELAY]; set => floatParameters[(int)DSP_ECHO.DELAY] = value; }
        public float feedback { get => floatParameters[(int)DSP_ECHO.FEEDBACK]; set => floatParameters[(int)DSP_ECHO.FEEDBACK] = value; }
        public float dryLevel { get => floatParameters[(int)DSP_ECHO.DRYLEVEL]; set => floatParameters[(int)DSP_ECHO.DRYLEVEL] = value; }
        public float wetLevel { get => floatParameters[(int)DSP_ECHO.WETLEVEL]; set => floatParameters[(int)DSP_ECHO.WETLEVEL] = value; }
        public EchoDelayChangeMode delayChangeMode { get => (EchoDelayChangeMode)intParameters[(int)DSP_ECHO.DELAYCHANGEMODE]; set => intParameters[(int)DSP_ECHO.DELAYCHANGEMODE] = (int)value; }
    }
}
