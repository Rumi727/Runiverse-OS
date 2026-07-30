#nullable enable
using FMOD;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in transceiver DSP.<br/>
    /// FMOD 내장 트랜시버 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class TransceiverDSP : DSP
    {
        TransceiverDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.TRANSCEIVER;

        public bool transmit { get => boolParameters[(int)DSP_TRANSCEIVER.TRANSMIT]; set => boolParameters[(int)DSP_TRANSCEIVER.TRANSMIT] = value; }
        public float gain { get => floatParameters[(int)DSP_TRANSCEIVER.GAIN]; set => floatParameters[(int)DSP_TRANSCEIVER.GAIN] = value; }
        public int channel { get => intParameters[(int)DSP_TRANSCEIVER.CHANNEL]; set => intParameters[(int)DSP_TRANSCEIVER.CHANNEL] = value; }
        public TransceiverSpeakerMode transmitSpeakerMode { get => (TransceiverSpeakerMode)intParameters[(int)DSP_TRANSCEIVER.TRANSMITSPEAKERMODE]; set => intParameters[(int)DSP_TRANSCEIVER.TRANSMITSPEAKERMODE] = (int)value; }
    }
}
