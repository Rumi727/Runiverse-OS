#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in per-channel delay DSP.<br/>
    /// FMOD 내장 채널별 딜레이 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class DelayDSP : DSP
    {
        DelayDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.DELAY;

        public float maxDelay { get => floatParameters[(int)DSP_DELAY.MAXDELAY]; set => floatParameters[(int)DSP_DELAY.MAXDELAY] = value; }

        /// <summary>
        /// Gets delay time for <paramref name="channel"/>.<br/>
        /// <paramref name="channel"/>의 지연 시간을 가져옵니다.
        /// </summary>
        public float GetDelay(int channel) => floatParameters[GetChannelParameterIndex(channel)];

        /// <summary>
        /// Sets delay time for <paramref name="channel"/>.<br/>
        /// <paramref name="channel"/>의 지연 시간을 설정합니다.
        /// </summary>
        public void SetDelay(int channel, float delay) => floatParameters[GetChannelParameterIndex(channel)] = delay;

        static int GetChannelParameterIndex(int channel)
        {
            if ((uint)channel >= 16)
                throw new ArgumentOutOfRangeException(nameof(channel));

            return (int)DSP_DELAY.CH0 + channel;
        }
    }
}
