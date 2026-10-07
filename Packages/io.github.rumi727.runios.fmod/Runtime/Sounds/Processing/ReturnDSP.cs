#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in return DSP.<br/>
    /// FMOD 내장 리턴 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ReturnDSP : DSP
    {
        ReturnDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.RETURN;

        public int id => intParameters[(int)DSP_RETURN.ID];
        public SoundSpeakerMode inputSpeakerMode { get => (SoundSpeakerMode)intParameters[(int)DSP_RETURN.INPUT_SPEAKER_MODE]; set => intParameters[(int)DSP_RETURN.INPUT_SPEAKER_MODE] = (int)value; }
    }
}
