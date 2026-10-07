#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in FFT DSP.<br/>
    /// FMOD 내장 FFT DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class FFTDSP : DSP
    {
        FFTDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.FFT;

        public int windowSize { get => intParameters[(int)DSP_FFT.WINDOWSIZE]; set => intParameters[(int)DSP_FFT.WINDOWSIZE] = value; }
        public FFTWindow window { get => (FFTWindow)intParameters[(int)DSP_FFT.WINDOW]; set => intParameters[(int)DSP_FFT.WINDOW] = (int)value; }
        public float bandStartFrequency { get => floatParameters[(int)DSP_FFT.BAND_START_FREQ]; set => floatParameters[(int)DSP_FFT.BAND_START_FREQ] = value; }
        public float bandStopFrequency { get => floatParameters[(int)DSP_FFT.BAND_STOP_FREQ]; set => floatParameters[(int)DSP_FFT.BAND_STOP_FREQ] = value; }
        public float[][] spectrum => GetFFTSpectrumDataParameter((int)DSP_FFT.SPECTRUMDATA);
        public float rms => floatParameters[(int)DSP_FFT.RMS];
        public float spectralCentroid => floatParameters[(int)DSP_FFT.SPECTRAL_CENTROID];
        public bool immediateMode { get => boolParameters[(int)DSP_FFT.IMMEDIATE_MODE]; set => boolParameters[(int)DSP_FFT.IMMEDIATE_MODE] = value; }
        public FFTDownmix downmix { get => (FFTDownmix)intParameters[(int)DSP_FFT.DOWNMIX]; set => intParameters[(int)DSP_FFT.DOWNMIX] = (int)value; }
        public int channel { get => intParameters[(int)DSP_FFT.CHANNEL]; set => intParameters[(int)DSP_FFT.CHANNEL] = value; }
    }
}
