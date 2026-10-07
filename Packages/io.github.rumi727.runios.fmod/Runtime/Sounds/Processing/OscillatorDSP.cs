#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in oscillator DSP.<br/>
    /// FMOD 내장 오실레이터 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class OscillatorDSP : DSP
    {
        OscillatorDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.OSCILLATOR;

        public OscillatorWaveform waveform { get => (OscillatorWaveform)intParameters[(int)DSP_OSCILLATOR.TYPE]; set => intParameters[(int)DSP_OSCILLATOR.TYPE] = (int)value; }
        public float rate { get => floatParameters[(int)DSP_OSCILLATOR.RATE]; set => floatParameters[(int)DSP_OSCILLATOR.RATE] = value; }
    }
}
