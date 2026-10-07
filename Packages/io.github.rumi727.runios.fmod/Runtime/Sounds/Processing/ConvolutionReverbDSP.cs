#nullable enable
using FMOD;
using System.Runtime.InteropServices;

namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Safely owns FMOD built-in convolution reverb DSP.<br/>
    /// FMOD 내장 컨볼루션 리버브 DSP를 안전하게 소유합니다.
    /// </summary>
    public sealed class ConvolutionReverbDSP : DSP
    {
        ConvolutionReverbDSP() { }
        internal override DSP_TYPE type => DSP_TYPE.CONVOLUTIONREVERB;

        public float wetLevel { get => floatParameters[(int)DSP_CONVOLUTION_REVERB.WET]; set => floatParameters[(int)DSP_CONVOLUTION_REVERB.WET] = value; }
        public float dryLevel { get => floatParameters[(int)DSP_CONVOLUTION_REVERB.DRY]; set => floatParameters[(int)DSP_CONVOLUTION_REVERB.DRY] = value; }
        public bool linked { get => boolParameters[(int)DSP_CONVOLUTION_REVERB.LINKED]; set => boolParameters[(int)DSP_CONVOLUTION_REVERB.LINKED] = value; }

        /// <summary>
        /// Replaces impulse response with interleaved signed 16-bit PCM samples.<br/>
        /// 인터리브된 부호 있는 16비트 PCM 샘플로 임펄스 응답을 교체합니다.
        /// </summary>
        public void SetImpulseResponse(short channelCount, ReadOnlySpan<short> samples)
        {
            if (channelCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(channelCount));

            byte[] pcm = MemoryMarshal.AsBytes(samples).ToArray();
            byte[] data = new byte[sizeof(short) + pcm.Length];
            BitConverter.GetBytes(channelCount).CopyTo(data, 0);
            Buffer.BlockCopy(pcm, 0, data, sizeof(short), pcm.Length);
            dataParameters[(int)DSP_CONVOLUTION_REVERB.IR] = data;
        }
    }
}
