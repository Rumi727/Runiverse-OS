#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in loudness meter DSP.<br/>
    /// FMOD 내장 라우드니스 미터 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class LoudnessMeterDSP : DSP
    {
        LoudnessMeterDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.LOUDNESS_METER;

        public LoudnessMeterState state { get => (LoudnessMeterState)intParameters[(int)DSP_LOUDNESS_METER.STATE]; set => intParameters[(int)DSP_LOUDNESS_METER.STATE] = (int)value; }

        public LoudnessMeterWeighting weighting
        {
            get
            {
                DSP_LOUDNESS_METER_WEIGHTING_TYPE value = GetDataParameter<DSP_LOUDNESS_METER_WEIGHTING_TYPE>((int)DSP_LOUDNESS_METER.WEIGHTING);
                return new LoudnessMeterWeighting((float[])value.channelweight.Clone());
            }
            set
            {
                if (value.channelWeights == null || value.channelWeights.Length != 32)
                    throw new ArgumentException("Channel weights must contain exactly 32 entries.", nameof(value));

                SetDataParameter((int)DSP_LOUDNESS_METER.WEIGHTING, new DSP_LOUDNESS_METER_WEIGHTING_TYPE { channelweight = (float[])value.channelWeights.Clone() });
            }
        }

        public LoudnessMeterInfo info
        {
            get
            {
                DSP_LOUDNESS_METER_INFO_TYPE value = GetDataParameter<DSP_LOUDNESS_METER_INFO_TYPE>((int)DSP_LOUDNESS_METER.INFO);
                return new LoudnessMeterInfo(
                    value.momentaryloudness,
                    value.shorttermloudness,
                    value.integratedloudness,
                    value.loudness10thpercentile,
                    value.loudness95thpercentile,
                    (float[])value.loudnesshistogram.Clone(),
                    value.maxtruepeak,
                    value.maxmomentaryloudness);
            }
        }
    }
}
