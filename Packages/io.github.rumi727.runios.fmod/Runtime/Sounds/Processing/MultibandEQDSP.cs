#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in five-band equalizer DSP.<br/>
    /// FMOD 내장 5밴드 이퀄라이저 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class MultibandEQDSP : DSP
    {
        MultibandEQDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.MULTIBAND_EQ;

        /// <summary>
        /// Gets filter type for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 필터 형식을 가져옵니다.
        /// </summary>
        public EqualizerFilter GetFilter(EqualizerBand band) => (EqualizerFilter)intParameters[GetParameterIndex(band, 0)];

        /// <summary>
        /// Sets filter type for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 필터 형식을 설정합니다.
        /// </summary>
        public void SetFilter(EqualizerBand band, EqualizerFilter filter) => intParameters[GetParameterIndex(band, 0)] = (int)filter;

        /// <summary>
        /// Gets frequency in hertz for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 헤르츠 단위 주파수를 가져옵니다.
        /// </summary>
        public float GetFrequency(EqualizerBand band) => floatParameters[GetParameterIndex(band, 1)];

        /// <summary>
        /// Sets frequency in hertz for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 헤르츠 단위 주파수를 설정합니다.
        /// </summary>
        public void SetFrequency(EqualizerBand band, float frequency) => floatParameters[GetParameterIndex(band, 1)] = frequency;

        /// <summary>
        /// Gets Q value for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 Q 값을 가져옵니다.
        /// </summary>
        public float GetQ(EqualizerBand band) => floatParameters[GetParameterIndex(band, 2)];

        /// <summary>
        /// Sets Q value for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 Q 값을 설정합니다.
        /// </summary>
        public void SetQ(EqualizerBand band, float q) => floatParameters[GetParameterIndex(band, 2)] = q;

        /// <summary>
        /// Gets gain in decibels for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 데시벨 단위 게인을 가져옵니다.
        /// </summary>
        public float GetGain(EqualizerBand band) => floatParameters[GetParameterIndex(band, 3)];

        /// <summary>
        /// Sets gain in decibels for <paramref name="band"/>.<br/>
        /// <paramref name="band"/>의 데시벨 단위 게인을 설정합니다.
        /// </summary>
        public void SetGain(EqualizerBand band, float gain) => floatParameters[GetParameterIndex(band, 3)] = gain;

        static int GetParameterIndex(EqualizerBand band, int parameter) => (int)DSP_MULTIBAND_EQ.A_FILTER + ((int)band * 4) + parameter;
    }
}
