#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Specifies sample encoding for raw PCM audio data.<br/>
    /// raw PCM 오디오 데이터의 샘플 인코딩을 지정합니다.
    /// </summary>
    public enum PCMFormat
    {
        /// <summary>
        /// 8-bit PCM samples.<br/>
        /// 8비트 PCM 샘플입니다.
        /// </summary>
        PCM8 = SOUND_FORMAT.PCM8,

        /// <summary>
        /// 16-bit PCM samples.<br/>
        /// 16비트 PCM 샘플입니다.
        /// </summary>
        PCM16 = SOUND_FORMAT.PCM16,

        /// <summary>
        /// 24-bit PCM samples.<br/>
        /// 24비트 PCM 샘플입니다.
        /// </summary>
        PCM24 = SOUND_FORMAT.PCM24,

        /// <summary>
        /// 32-bit PCM samples.<br/>
        /// 32비트 PCM 샘플입니다.
        /// </summary>
        PCM32 = SOUND_FORMAT.PCM32,

        /// <summary>
        /// 32-bit floating-point PCM samples.<br/>
        /// 32비트 부동 소수점 PCM 샘플입니다.
        /// </summary>
        Float = SOUND_FORMAT.PCMFLOAT
    }

    public static class PCMFormatExtension
    {
        public static PCMFormat? ToPCMFormat(this SOUND_FORMAT format)
        {
            return format switch
            {
                SOUND_FORMAT.PCM8 => PCMFormat.PCM8,
                SOUND_FORMAT.PCM16 => PCMFormat.PCM16,
                SOUND_FORMAT.PCM24 => PCMFormat.PCM24,
                SOUND_FORMAT.PCM32 => PCMFormat.PCM32,
                SOUND_FORMAT.PCMFLOAT => PCMFormat.Float,
                _ => null
            };
        }
    }
}