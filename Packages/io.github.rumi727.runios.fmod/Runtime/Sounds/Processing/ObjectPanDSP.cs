#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in object pan DSP.<br/>
    /// FMOD 내장 오브젝트 팬 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ObjectPanDSP : DSP
    {
        ObjectPanDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.OBJECTPAN;

        public DSP3DAttributesMulti spatialAttributes
        {
            get => PanDSP.ToManaged(GetDataParameter<DSP_PARAMETER_3DATTRIBUTES_MULTI>((int)DSP_OBJECTPAN._3D_POSITION));
            set => SetDataParameter((int)DSP_OBJECTPAN._3D_POSITION, PanDSP.ToNative(value));
        }

        public PanRolloff rolloff { get => (PanRolloff)intParameters[(int)DSP_OBJECTPAN._3D_ROLLOFF]; set => intParameters[(int)DSP_OBJECTPAN._3D_ROLLOFF] = (int)value; }
        public float minDistance { get => floatParameters[(int)DSP_OBJECTPAN._3D_MIN_DISTANCE]; set => floatParameters[(int)DSP_OBJECTPAN._3D_MIN_DISTANCE] = value; }
        public float maxDistance { get => floatParameters[(int)DSP_OBJECTPAN._3D_MAX_DISTANCE]; set => floatParameters[(int)DSP_OBJECTPAN._3D_MAX_DISTANCE] = value; }
        public PanExtentMode extentMode { get => (PanExtentMode)intParameters[(int)DSP_OBJECTPAN._3D_EXTENT_MODE]; set => intParameters[(int)DSP_OBJECTPAN._3D_EXTENT_MODE] = (int)value; }
        public float soundSize { get => floatParameters[(int)DSP_OBJECTPAN._3D_SOUND_SIZE]; set => floatParameters[(int)DSP_OBJECTPAN._3D_SOUND_SIZE] = value; }
        public float minExtent { get => floatParameters[(int)DSP_OBJECTPAN._3D_MIN_EXTENT]; set => floatParameters[(int)DSP_OBJECTPAN._3D_MIN_EXTENT] = value; }
        public (float linear, float additive) overallGain => GetOverallGainDataParameter((int)DSP_OBJECTPAN.OVERALL_GAIN);
        public float outputGain { get => floatParameters[(int)DSP_OBJECTPAN.OUTPUTGAIN]; set => floatParameters[(int)DSP_OBJECTPAN.OUTPUTGAIN] = value; }
        public AttenuationRange attenuationRange => PanDSP.ToManaged(GetDataParameter<DSP_PARAMETER_ATTENUATION_RANGE>((int)DSP_OBJECTPAN.ATTENUATION_RANGE));
        public bool overrideRange { get => boolParameters[(int)DSP_OBJECTPAN.OVERRIDE_RANGE]; set => boolParameters[(int)DSP_OBJECTPAN.OVERRIDE_RANGE] = value; }
    }
}
