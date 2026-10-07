#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in send DSP.<br/>
    /// FMOD 내장 센드 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class SendDSP : DSP
    {
        SendDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.SEND;

        public int returnID { get => intParameters[(int)DSP_SEND.RETURNID]; set => intParameters[(int)DSP_SEND.RETURNID] = value; }
        public float level { get => floatParameters[(int)DSP_SEND.LEVEL]; set => floatParameters[(int)DSP_SEND.LEVEL] = value; }
    }
}
