#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in channel mix DSP.<br/>
    /// FMOD 내장 채널 믹스 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ChannelMixDSP : DSP
    {
        ChannelMixDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.CHANNELMIX;

        public ChannelMixOutput output { get => (ChannelMixOutput)intParameters[(int)DSP_CHANNELMIX.OUTPUTGROUPING]; set => intParameters[(int)DSP_CHANNELMIX.OUTPUTGROUPING] = (int)value; }

        /// <summary>
        /// Gets gain in decibels for <paramref name="channel"/>.<br/>
        /// <paramref name="channel"/>의 데시벨 단위 게인을 가져옵니다.
        /// </summary>
        public float GetGain(int channel) => floatParameters[GetGainParameterIndex(channel)];

        /// <summary>
        /// Sets gain in decibels for <paramref name="channel"/>.<br/>
        /// <paramref name="channel"/>의 데시벨 단위 게인을 설정합니다.
        /// </summary>
        public void SetGain(int channel, float gain) => floatParameters[GetGainParameterIndex(channel)] = gain;

        /// <summary>
        /// Gets output channel mapped from <paramref name="inputChannel"/>.<br/>
        /// <paramref name="inputChannel"/>에서 매핑된 출력 채널을 가져옵니다.
        /// </summary>
        public int GetOutputChannel(int inputChannel) => intParameters[GetOutputParameterIndex(inputChannel)];

        /// <summary>
        /// Maps <paramref name="inputChannel"/> to <paramref name="outputChannel"/>.<br/>
        /// <paramref name="inputChannel"/>을 <paramref name="outputChannel"/>에 매핑합니다.
        /// </summary>
        public void SetOutputChannel(int inputChannel, int outputChannel) => intParameters[GetOutputParameterIndex(inputChannel)] = outputChannel;

        static int GetGainParameterIndex(int channel)
        {
            if ((uint)channel >= 32)
                throw new ArgumentOutOfRangeException(nameof(channel));

            return (int)DSP_CHANNELMIX.GAIN_CH0 + channel;
        }

        static int GetOutputParameterIndex(int channel)
        {
            if ((uint)channel >= 32)
                throw new ArgumentOutOfRangeException(nameof(channel));

            return (int)DSP_CHANNELMIX.OUTPUT_CH0 + channel;
        }
    }
}
