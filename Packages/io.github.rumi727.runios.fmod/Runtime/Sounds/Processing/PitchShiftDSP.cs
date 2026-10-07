#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in pitch-shift DSP.<br/>
    /// FMOD 내장 피치 시프트 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class PitchShiftDSP : DSP
    {
        PitchShiftDSP() { }

        internal override DSP_TYPE type => DSP_TYPE.PITCHSHIFT;

        /// <summary>
        /// Gets or sets pitch-shift multiplier.<br/>
        /// 피치 시프트 배율을 가져오거나 설정합니다.
        /// </summary>
        public float pitch
        {
            get => floatParameters[(int)DSP_PITCHSHIFT.PITCH];
            set => floatParameters[(int)DSP_PITCHSHIFT.PITCH] = value;
        }

        /// <summary>
        /// Gets or sets FFT window size.<br/>
        /// FFT 윈도우 크기를 가져오거나 설정합니다.
        /// </summary>
        public float fftSize
        {
            get => floatParameters[(int)DSP_PITCHSHIFT.FFTSIZE];
            set => floatParameters[(int)DSP_PITCHSHIFT.FFTSIZE] = value;
        }

        /// <summary>
        /// Gets or sets maximum processed channel count.<br/>
        /// 처리할 최대 채널 수를 가져오거나 설정합니다.
        /// </summary>
        public float maxChannels
        {
            get => floatParameters[(int)DSP_PITCHSHIFT.MAXCHANNELS];
            set => floatParameters[(int)DSP_PITCHSHIFT.MAXCHANNELS] = value;
        }
    }
}
