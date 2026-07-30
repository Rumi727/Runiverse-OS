#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in three-band dynamics DSP.<br/>
    /// FMOD 내장 3밴드 다이내믹스 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class MultibandDynamicsDSP : DSP
    {
        MultibandDynamicsDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.MULTIBAND_DYNAMICS;

        public float lowerFrequency { get => floatParameters[(int)DSP_MULTIBAND_DYNAMICS.LOWER_FREQUENCY]; set => floatParameters[(int)DSP_MULTIBAND_DYNAMICS.LOWER_FREQUENCY] = value; }
        public float upperFrequency { get => floatParameters[(int)DSP_MULTIBAND_DYNAMICS.UPPER_FREQUENCY]; set => floatParameters[(int)DSP_MULTIBAND_DYNAMICS.UPPER_FREQUENCY] = value; }
        public bool linked { get => boolParameters[(int)DSP_MULTIBAND_DYNAMICS.LINKED]; set => boolParameters[(int)DSP_MULTIBAND_DYNAMICS.LINKED] = value; }
        public bool useSidechain { get => GetBooleanDataParameter((int)DSP_MULTIBAND_DYNAMICS.USE_SIDECHAIN); set => SetBooleanDataParameter((int)DSP_MULTIBAND_DYNAMICS.USE_SIDECHAIN, value); }

        public MultibandDynamicsMode GetMode(MultibandDynamicsBand band) => (MultibandDynamicsMode)intParameters[GetParameterIndex(band, 0)];
        public void SetMode(MultibandDynamicsBand band, MultibandDynamicsMode mode) => intParameters[GetParameterIndex(band, 0)] = (int)mode;
        public float GetGain(MultibandDynamicsBand band) => floatParameters[GetParameterIndex(band, 1)];
        public void SetGain(MultibandDynamicsBand band, float gain) => floatParameters[GetParameterIndex(band, 1)] = gain;
        public float GetThreshold(MultibandDynamicsBand band) => floatParameters[GetParameterIndex(band, 2)];
        public void SetThreshold(MultibandDynamicsBand band, float threshold) => floatParameters[GetParameterIndex(band, 2)] = threshold;
        public float GetRatio(MultibandDynamicsBand band) => floatParameters[GetParameterIndex(band, 3)];
        public void SetRatio(MultibandDynamicsBand band, float ratio) => floatParameters[GetParameterIndex(band, 3)] = ratio;
        public float GetAttack(MultibandDynamicsBand band) => floatParameters[GetParameterIndex(band, 4)];
        public void SetAttack(MultibandDynamicsBand band, float attack) => floatParameters[GetParameterIndex(band, 4)] = attack;
        public float GetRelease(MultibandDynamicsBand band) => floatParameters[GetParameterIndex(band, 5)];
        public void SetRelease(MultibandDynamicsBand band, float release) => floatParameters[GetParameterIndex(band, 5)] = release;
        public float GetMakeupGain(MultibandDynamicsBand band) => floatParameters[GetParameterIndex(band, 6)];
        public void SetMakeupGain(MultibandDynamicsBand band, float gain) => floatParameters[GetParameterIndex(band, 6)] = gain;
        public float[] GetResponse(MultibandDynamicsBand band) => GetDynamicResponseDataParameter(GetParameterIndex(band, 7));

        static int GetParameterIndex(MultibandDynamicsBand band, int parameter) => (int)DSP_MULTIBAND_DYNAMICS.A_MODE + ((int)band * 8) + parameter;
    }
}
