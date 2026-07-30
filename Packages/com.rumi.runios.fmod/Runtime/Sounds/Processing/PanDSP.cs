#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in pan DSP.<br/>
    /// FMOD 내장 팬 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class PanDSP : DSP
    {
        PanDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.PAN;

        public PanMode mode { get => (PanMode)intParameters[(int)DSP_PAN.MODE]; set => intParameters[(int)DSP_PAN.MODE] = (int)value; }
        public float stereoPosition { get => floatParameters[(int)DSP_PAN._2D_STEREO_POSITION]; set => floatParameters[(int)DSP_PAN._2D_STEREO_POSITION] = value; }
        public float direction { get => floatParameters[(int)DSP_PAN._2D_DIRECTION]; set => floatParameters[(int)DSP_PAN._2D_DIRECTION] = value; }
        public float extent { get => floatParameters[(int)DSP_PAN._2D_EXTENT]; set => floatParameters[(int)DSP_PAN._2D_EXTENT] = value; }
        public float rotation { get => floatParameters[(int)DSP_PAN._2D_ROTATION]; set => floatParameters[(int)DSP_PAN._2D_ROTATION] = value; }
        public float lfeLevel { get => floatParameters[(int)DSP_PAN._2D_LFE_LEVEL]; set => floatParameters[(int)DSP_PAN._2D_LFE_LEVEL] = value; }
        public PanStereoMode stereoMode { get => (PanStereoMode)intParameters[(int)DSP_PAN._2D_STEREO_MODE]; set => intParameters[(int)DSP_PAN._2D_STEREO_MODE] = (int)value; }
        public float stereoSeparation { get => floatParameters[(int)DSP_PAN._2D_STEREO_SEPARATION]; set => floatParameters[(int)DSP_PAN._2D_STEREO_SEPARATION] = value; }
        public float stereoAxis { get => floatParameters[(int)DSP_PAN._2D_STEREO_AXIS]; set => floatParameters[(int)DSP_PAN._2D_STEREO_AXIS] = value; }
        public int enabledSpeakerMask { get => intParameters[(int)DSP_PAN.ENABLED_SPEAKERS]; set => intParameters[(int)DSP_PAN.ENABLED_SPEAKERS] = value; }

        public DSP3DAttributesMulti spatialAttributes
        {
            get => ToManaged(GetDataParameter<DSP_PARAMETER_3DATTRIBUTES_MULTI>((int)DSP_PAN._3D_POSITION));
            set => SetDataParameter((int)DSP_PAN._3D_POSITION, ToNative(value));
        }

        public PanRolloff rolloff { get => (PanRolloff)intParameters[(int)DSP_PAN._3D_ROLLOFF]; set => intParameters[(int)DSP_PAN._3D_ROLLOFF] = (int)value; }
        public float minDistance { get => floatParameters[(int)DSP_PAN._3D_MIN_DISTANCE]; set => floatParameters[(int)DSP_PAN._3D_MIN_DISTANCE] = value; }
        public float maxDistance { get => floatParameters[(int)DSP_PAN._3D_MAX_DISTANCE]; set => floatParameters[(int)DSP_PAN._3D_MAX_DISTANCE] = value; }
        public PanExtentMode extentMode { get => (PanExtentMode)intParameters[(int)DSP_PAN._3D_EXTENT_MODE]; set => intParameters[(int)DSP_PAN._3D_EXTENT_MODE] = (int)value; }
        public float soundSize { get => floatParameters[(int)DSP_PAN._3D_SOUND_SIZE]; set => floatParameters[(int)DSP_PAN._3D_SOUND_SIZE] = value; }
        public float minExtent { get => floatParameters[(int)DSP_PAN._3D_MIN_EXTENT]; set => floatParameters[(int)DSP_PAN._3D_MIN_EXTENT] = value; }
        public float panBlend { get => floatParameters[(int)DSP_PAN._3D_PAN_BLEND]; set => floatParameters[(int)DSP_PAN._3D_PAN_BLEND] = value; }
        public bool lfeUpmix { get => intParameters[(int)DSP_PAN.LFE_UPMIX_ENABLED] != 0; set => intParameters[(int)DSP_PAN.LFE_UPMIX_ENABLED] = value ? 1 : 0; }
        public (float linear, float additive) overallGain => GetOverallGainDataParameter((int)DSP_PAN.OVERALL_GAIN);
        public SoundSpeakerMode surroundSpeakerMode { get => (SoundSpeakerMode)intParameters[(int)DSP_PAN.SURROUND_SPEAKER_MODE]; set => intParameters[(int)DSP_PAN.SURROUND_SPEAKER_MODE] = (int)value; }
        public float heightBlend { get => floatParameters[(int)DSP_PAN._2D_HEIGHT_BLEND]; set => floatParameters[(int)DSP_PAN._2D_HEIGHT_BLEND] = value; }
        public AttenuationRange attenuationRange => ToManaged(GetDataParameter<DSP_PARAMETER_ATTENUATION_RANGE>((int)DSP_PAN.ATTENUATION_RANGE));
        public bool overrideRange { get => boolParameters[(int)DSP_PAN.OVERRIDE_RANGE]; set => boolParameters[(int)DSP_PAN.OVERRIDE_RANGE] = value; }

        internal static DSP3DAttributesMulti ToManaged(DSP_PARAMETER_3DATTRIBUTES_MULTI value)
        {
            int count = Math.Clamp(value.numlisteners, 0, 8);
            DSP3DAttributes[] relative = new DSP3DAttributes[count];
            float[] weights = new float[count];

            for (int index = 0; index < count; index++)
            {
                relative[index] = ToDSP3DAttributes(value.relative[index]);
                weights[index] = value.weight[index];
            }

            return new DSP3DAttributesMulti(relative, weights, ToDSP3DAttributes(value.absolute));
        }

        internal static DSP_PARAMETER_3DATTRIBUTES_MULTI ToNative(DSP3DAttributesMulti value)
        {
            if (value.relative == null || value.listenerWeights == null || value.relative.Length != value.listenerWeights.Length || value.relative.Length is < 1 or > 8)
                throw new ArgumentException("Relative attributes and listener weights must contain the same number of one to eight entries.", nameof(value));

            ATTRIBUTES_3D[] relative = new ATTRIBUTES_3D[8];
            float[] weights = new float[8];

            for (int index = 0; index < value.relative.Length; index++)
            {
                relative[index] = ToFMOD3DAttributes(value.relative[index]);
                weights[index] = value.listenerWeights[index];
            }

            return new DSP_PARAMETER_3DATTRIBUTES_MULTI
            {
                numlisteners = value.relative.Length,
                relative = relative,
                weight = weights,
                absolute = ToFMOD3DAttributes(value.absolute)
            };
        }

        internal static AttenuationRange ToManaged(DSP_PARAMETER_ATTENUATION_RANGE value) => new(value.min, value.max);
    }
}
