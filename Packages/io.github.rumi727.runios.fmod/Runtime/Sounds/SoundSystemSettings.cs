#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Provides settings applied before a <see cref="SoundSystem"/> initializes its FMOD system.<br/>
    /// <see cref="SoundSystem"/>이 FMOD 시스템을 초기화하기 전에 적용할 설정을 제공합니다.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/> property preserves the current setting during <see cref="SoundSystem.Reset(SoundSystemSettings)"/>.<br/>
    /// In a constructor, <see langword="null"/> uses the wrapper or FMOD default.
    /// <br/><br/>
    /// <see cref="SoundSystem.Reset(SoundSystemSettings)"/>에서는 <see langword="null"/>인 속성이 현재 설정을 유지합니다.<br/>
    /// 생성자에서는 <see langword="null"/>인 속성이 래퍼 또는 FMOD 기본값을 사용합니다.
    /// </remarks>
    public readonly record struct SoundSystemSettings
    {
        /// <summary>
        /// Gets the maximum number of virtual channels passed to FMOD initialization.<br/>
        /// FMOD 초기화에 전달할 최대 가상 채널 수를 가져옵니다.
        /// </summary>
        public int? maxChannels { get; init; }

        /// <summary>
        /// Gets the flags passed to FMOD initialization.<br/>
        /// FMOD 초기화에 전달할 플래그를 가져옵니다.
        /// </summary>
        public INITFLAGS? initFlags { get; init; }

        /// <summary>
        /// Gets the maximum number of software-mixed real channels, or <see langword="null"/> to use the current setting.<br/>
        /// 소프트웨어로 믹싱할 최대 실제 채널 수를 가져오며, 현재 설정을 사용하려면 <see langword="null"/>입니다.
        /// </summary>
        public int? softwareChannels { get; init; }

        /// <summary>
        /// Gets the software mixer format, or <see langword="null"/> to use the current setting.<br/>
        /// 소프트웨어 믹서 포맷을 가져오며, 현재 설정을 사용하려면 <see langword="null"/>입니다.
        /// </summary>
        public SoundSystemSoftwareFormat? softwareFormat { get; init; }

        /// <summary>
        /// Gets the DSP mixer buffer configuration, or <see langword="null"/> to use the current setting.<br/>
        /// DSP 믹서 버퍼 구성을 가져오며, 현재 설정을 사용하려면 <see langword="null"/>입니다.
        /// </summary>
        public SoundSystemDSPBuffer? dspBuffer { get; init; }
    }

    /// <summary>
    /// Describes the software mixer output format applied before FMOD initialization.<br/>
    /// FMOD 초기화 전에 적용할 소프트웨어 믹서 출력 포맷을 설명합니다.
    /// </summary>
    /// <param name="sampleRate">
    /// The mixer sample rate in hertz.<br/>
    /// 믹서 샘플 레이트이며 단위는 헤르츠입니다.
    /// </param>
    /// <param name="speakerMode">
    /// The mixer speaker configuration.<br/>
    /// 믹서 스피커 구성입니다.
    /// </param>
    /// <param name="rawSpeakerCount">
    /// The speaker count used when <paramref name="speakerMode"/> is <see cref="SPEAKERMODE.RAW"/>.<br/>
    /// <paramref name="speakerMode"/>가 <see cref="SPEAKERMODE.RAW"/>일 때 사용할 스피커 수입니다.
    /// </param>
    public readonly record struct SoundSystemSoftwareFormat(int sampleRate, SPEAKERMODE speakerMode, int rawSpeakerCount = 0);

    /// <summary>
    /// Describes the DSP mixer buffer configuration applied before FMOD initialization.<br/>
    /// FMOD 초기화 전에 적용할 DSP 믹서 버퍼 구성을 설명합니다.
    /// </summary>
    /// <param name="length">
    /// The length of one DSP buffer in samples.<br/>
    /// DSP 버퍼 하나의 길이이며 단위는 샘플입니다.
    /// </param>
    /// <param name="count">
    /// The number of DSP buffers in the mixer ring buffer.<br/>
    /// 믹서 링 버퍼를 구성하는 DSP 버퍼 수입니다.
    /// </param>
    public readonly record struct SoundSystemDSPBuffer(uint length, int count);
}
