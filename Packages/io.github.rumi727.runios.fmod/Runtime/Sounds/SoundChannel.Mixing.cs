#nullable enable
using RuniOS.Sounds.Processing;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
        readonly ReaderWriterLockSlim mixingLock = new();

        /// <summary>
        /// Gets or sets this channel's volume.<br/>
        /// 이 채널의 볼륨을 가져오거나 설정합니다.
        /// </summary>
        public float volume
        {
            get
            {
                native.getVolume(out float volume).ThrowIfNotOkOfChannel();
                return volume;
            }
            set => native.setVolume(value).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets or sets this channel's pitch multiplier.<br/>
        /// 이 채널의 피치 배율을 가져오거나 설정합니다.
        /// </summary>
        public float pitch
        {
            get
            {
                native.getPitch(out float pitch).ThrowIfNotOkOfChannel();
                return pitch;
            }
            set => native.setPitch(value).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets or sets whether this channel is muted.<br/>
        /// 이 채널이 음소거되었는지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool mute
        {
            get
            {
                native.getMute(out bool mute).ThrowIfNotOkOfChannel();
                return mute;
            }
            set => native.setMute(value).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets or sets whether FMOD ramps volume changes instead of applying them immediately.<br/>
        /// FMOD가 볼륨 변경을 즉시 적용하지 않고 램프 처리할지 여부를 가져오거나 설정합니다.
        /// </summary>
        public bool volumeRamp
        {
            get
            {
                native.getVolumeRamp(out bool volumeRamp).ThrowIfNotOkOfChannel();
                return volumeRamp;
            }
            set => native.setVolumeRamp(value).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets the calculated final audibility after FMOD applies attenuation and volume settings.<br/>
        /// FMOD가 감쇠와 볼륨 설정을 적용한 뒤 계산한 최종 가청도를 가져옵니다.
        /// </summary>
        public float audibility
        {
            get
            {
                native.getAudibility(out float audibility).ThrowIfNotOkOfChannel();
                return audibility;
            }
        }

        /// <summary>
        /// Gets or sets the dry-signal gain of the built-in low-pass filter.<br/>
        /// 내장 저역통과 필터의 드라이 신호 게인을 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// <see cref="SoundSystem.main"/> uses <c>INITFLAGS.NORMAL</c>, so FMOD's built-in per-channel low-pass filter is not enabled.<br/>
        /// Create <see cref="LowPassDSP"/> through <see cref="SoundSystem.CreateDSP{LowPassDSP}"/> and attach it with <see cref="DSP.Add(Processing.DSP, DSPIndex)"/> when filtering is needed.
        /// <br/><br/>
        /// <see cref="SoundSystem.main"/>은 <c>INITFLAGS.NORMAL</c>을 사용하므로 FMOD 내장 채널별 저역통과 필터를 활성화하지 않습니다.<br/>
        /// 필터링이 필요하면 <see cref="SoundSystem.CreateDSP{LowPassDSP}"/>로 <see cref="LowPassDSP"/>를 생성하고 <see cref="DSP.Add(Processing.DSP, DSPIndex)"/>로 부착하세요.
        /// </remarks>
        public float lowPassGain
        {
            get
            {
                native.getLowPassGain(out float gain).ThrowIfNotOkOfChannel();
                return gain;
            }
            set => native.setLowPassGain(value).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Gets indexed access to this channel's reverb wet/send levels.<br/>
        /// 이 채널의 리버브 웻/센드 레벨에 인덱스로 접근합니다.
        /// </summary>
        public ReverbWetLevel reverbWetLevel { get; }

        /// <summary>
        /// Sets the left/right pan level.<br/>
        /// 좌우 팬 레벨을 설정합니다.
        /// </summary>
        public float panStereo
        {
            set => native.setPan(value).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Sets incoming levels for each input channel of a multi-channel signal.<br/>
        /// 다중 채널 신호의 각 입력 채널에 적용할 레벨을 설정합니다.
        /// </summary>
        /// <param name="levels">
        /// The input-channel levels to apply.<br/>
        /// 적용할 입력 채널 레벨입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="levels"/> is <see langword="null"/>.<br/>
        /// <paramref name="levels"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public void SetInputMixLevels(float[] levels)
        {
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));

            native.setMixLevelsInput(levels, levels.Length).ThrowIfNotOkOfChannel();
        }

        /// <summary>
        /// Sets outgoing levels for each output speaker.<br/>
        /// 각 출력 스피커에 적용할 레벨을 설정합니다.
        /// </summary>
        /// <param name="frontLeft">
        /// The front-left speaker level.<br/>
        /// 전면 왼쪽 스피커 레벨입니다.
        /// </param>
        /// <param name="frontRight">
        /// The front-right speaker level.<br/>
        /// 전면 오른쪽 스피커 레벨입니다.
        /// </param>
        /// <param name="center">
        /// The center speaker level.<br/>
        /// 중앙 스피커 레벨입니다.
        /// </param>
        /// <param name="lfe">
        /// The low-frequency effects speaker level.<br/>
        /// 저주파 효과 스피커 레벨입니다.
        /// </param>
        /// <param name="surroundLeft">
        /// The surround-left speaker level.<br/>
        /// 서라운드 왼쪽 스피커 레벨입니다.
        /// </param>
        /// <param name="surroundRight">
        /// The surround-right speaker level.<br/>
        /// 서라운드 오른쪽 스피커 레벨입니다.
        /// </param>
        /// <param name="backLeft">
        /// The back-left speaker level.<br/>
        /// 후면 왼쪽 스피커 레벨입니다.
        /// </param>
        /// <param name="backRight">
        /// The back-right speaker level.<br/>
        /// 후면 오른쪽 스피커 레벨입니다.
        /// </param>
        public void SetOutputMixLevels(
            float frontLeft,
            float frontRight,
            float center,
            float lfe,
            float surroundLeft,
            float surroundRight,
            float backLeft,
            float backRight) => native.setMixLevelsOutput
        (
            frontLeft,
            frontRight,
            center,
            lfe,
            surroundLeft,
            surroundRight,
            backLeft,
            backRight
        ).ThrowIfNotOkOfChannel();

        /// <summary>
        /// Sets a pan matrix that maps input channels to output speakers.<br/>
        /// 입력 채널을 출력 스피커에 매핑하는 팬 매트릭스를 설정합니다.
        /// </summary>
        /// <param name="matrix">
        /// A row-major matrix, or <see langword="null"/> to restore FMOD's default matrix.<br/>
        /// 행 우선 매트릭스이며, <see langword="null"/>이면 FMOD 기본 매트릭스를 복원합니다.
        /// </param>
        /// <param name="outputChannels">
        /// The number of output channels, which is the number of matrix rows.<br/>
        /// 출력 채널 수이며 매트릭스 행 수와 같습니다.
        /// </param>
        /// <param name="inputChannels">
        /// The number of input channels represented by each matrix column.<br/>
        /// 각 매트릭스 열이 나타내는 입력 채널 수입니다.
        /// </param>
        /// <param name="inputChannelHop">
        /// The matrix row width, or zero to use <paramref name="inputChannels"/>.<br/>
        /// 매트릭스 행 너비이며, 0이면 <paramref name="inputChannels"/>를 사용합니다.
        /// </param>
        public void SetMixMatrix(float[]? matrix, int outputChannels, int inputChannels, int inputChannelHop = 0)
        {
            mixingLock.EnterWriteLock();

            try
            {
                native.setMixMatrix(matrix!, outputChannels, inputChannels, inputChannelHop).ThrowIfNotOkOfChannel();
            }
            finally
            {
                mixingLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Gets the current pan matrix and its output and input channel counts.<br/>
        /// 현재 팬 매트릭스와 출력 및 입력 채널 수를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The row-major matrix, output-channel count, and input-channel count.<br/>
        /// 행 우선 매트릭스, 출력 채널 수 및 입력 채널 수를 반환합니다.
        /// </returns>
        public (float[] matrix, int outputChannels, int inputChannels) GetMixMatrix()
        {
            mixingLock.EnterReadLock();

            try
            {
                native.getMixMatrix(null!, out int outputChannels, out int inputChannels).ThrowIfNotOkOfChannel();

                if (outputChannels == 0 || inputChannels == 0)
                    return (Array.Empty<float>(), outputChannels, inputChannels);

                float[] matrix = new float[checked(outputChannels * inputChannels)];
                native.getMixMatrix(matrix, out _, out _).ThrowIfNotOkOfChannel();
                return (matrix, outputChannels, inputChannels);
            }
            finally
            {
                mixingLock.ExitReadLock();
            }
        }
    }
}
