#nullable enable
namespace RuniOS.Sounds.Processing
{
    /// <summary>
    /// Selects an oscillator waveform.<br/>
    /// 오실레이터 파형을 선택합니다.
    /// </summary>
    public enum OscillatorWaveform { Sine, Square, SawUp, SawDown, Triangle, Noise }

    /// <summary>
    /// Selects how echo delay changes are smoothed.<br/>
    /// 에코 지연 시간 변경을 보간하는 방식을 선택합니다.
    /// </summary>
    public enum EchoDelayChangeMode { Fade, Lerp, None }

    /// <summary>
    /// Selects an output layout for a channel mixer.<br/>
    /// 채널 믹서의 출력 레이아웃을 선택합니다.
    /// </summary>
    public enum ChannelMixOutput { Default, AllMono, AllStereo, AllQuad, All5Point1, All7Point1, AllLfe, All7Point1Point4 }

    /// <summary>
    /// Selects a sound speaker layout.<br/>
    /// 사운드 스피커 레이아웃을 선택합니다.
    /// </summary>
    public enum SoundSpeakerMode { Default, Raw, Mono, Stereo, Quad, Surround, Surround5Point1, Surround7Point1, Surround7Point1Point4 }

    /// <summary>
    /// Selects a transceiver transmit speaker layout.<br/>
    /// 트랜시버 송신 스피커 레이아웃을 선택합니다.
    /// </summary>
    public enum TransceiverSpeakerMode { Auto = -1, Mono, Stereo, Surround }

    /// <summary>
    /// Selects a three-band equalizer crossover slope.<br/>
    /// 3밴드 이퀄라이저 크로스오버 기울기를 선택합니다.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public enum ThreeEQCrossoverSlope { dB12, dB24, dB48 }

    /// <summary>
    /// Identifies one multiband equalizer band.<br/>
    /// 멀티밴드 이퀄라이저 밴드 하나를 식별합니다.
    /// </summary>
    public enum EqualizerBand { A, B, C, D, E }

    /// <summary>
    /// Selects a multiband equalizer filter.<br/>
    /// 멀티밴드 이퀄라이저 필터를 선택합니다.
    /// </summary>
    public enum EqualizerFilter { Disabled, LowPass12dB, LowPass24dB, LowPass48dB, HighPass12dB, HighPass24dB, HighPass48dB, LowShelf, HighShelf, Peaking, BandPass, Notch, AllPass, LowPass6dB, HighPass6dB }

    /// <summary>
    /// Identifies one multiband dynamics band.<br/>
    /// 멀티밴드 다이내믹스 밴드 하나를 식별합니다.
    /// </summary>
    public enum MultibandDynamicsBand { A, B, C }

    /// <summary>
    /// Selects a multiband dynamics response mode.<br/>
    /// 멀티밴드 다이내믹스 반응 모드를 선택합니다.
    /// </summary>
    public enum MultibandDynamicsMode { Disabled, CompressUp, CompressDown, ExpandUp, ExpandDown }

    /// <summary>
    /// Selects an FFT window function.<br/>
    /// FFT 창 함수를 선택합니다.
    /// </summary>
    public enum FFTWindow { Rectangular, Triangle, Hamming, Hanning, Blackman, BlackmanHarris }

    /// <summary>
    /// Selects the FFT analysis downmix mode.<br/>
    /// FFT 분석 다운믹스 모드를 선택합니다.
    /// </summary>
    public enum FFTDownmix { None, Mono }

    /// <summary>
    /// Selects a pan DSP output mode.<br/>
    /// 팬 DSP 출력 모드를 선택합니다.
    /// </summary>
    public enum PanMode { mono, stereo, surround }

    /// <summary>
    /// Selects how stereo input is positioned in surround output.<br/>
    /// 스테레오 입력을 서라운드 출력에 배치하는 방식을 선택합니다.
    /// </summary>
    public enum PanStereoMode { distributed, discrete }

    /// <summary>
    /// Selects a 3D attenuation curve for a pan DSP.<br/>
    /// 팬 DSP의 3D 감쇠 곡선을 선택합니다.
    /// </summary>
    public enum PanRolloff { linearSquared, linear, inverse, inverseTapered, custom }

    /// <summary>
    /// Selects how a pan DSP calculates 3D sound extent.<br/>
    /// 팬 DSP가 3D 사운드 범위를 계산하는 방식을 선택합니다.
    /// </summary>
    public enum PanExtentMode { auto, user, off }

    /// <summary>
    /// Identifies the update state of a loudness meter.<br/>
    /// 라우드니스 미터의 갱신 상태를 식별합니다.
    /// </summary>
    public enum LoudnessMeterState { resetIntegrated = -3, resetMaxPeak = -2, resetAll = -1, paused, analyzing }

    /// <summary>
    /// Stores one DSP 3D transform.<br/>
    /// DSP 3D 변환 하나를 저장합니다.
    /// </summary>
    public struct DSP3DAttributes(Vector3 position, Vector3 velocity, Vector3 forward, Vector3 up)
    {
        /// <summary>
        /// The sound position.<br/>
        /// 사운드 위치입니다.
        /// </summary>
        public Vector3 position = position;

        /// <summary>
        /// The sound velocity.<br/>
        /// 사운드 속도입니다.
        /// </summary>
        public Vector3 velocity = velocity;

        /// <summary>
        /// The sound forward direction.<br/>
        /// 사운드 전방 방향입니다.
        /// </summary>
        public Vector3 forward = forward;

        /// <summary>
        /// The sound up direction.<br/>
        /// 사운드 상단 방향입니다.
        /// </summary>
        public Vector3 up = up;
    }

    /// <summary>
    /// Stores DSP 3D attributes for every listener and for the absolute sound position.<br/>
    /// 각 리스너 및 절대 사운드 위치의 DSP 3D 특성을 저장합니다.
    /// </summary>
    public struct DSP3DAttributesMulti(DSP3DAttributes[] relative, float[] listenerWeights, DSP3DAttributes absolute)
    {
        /// <summary>
        /// The attributes relative to each listener.<br/>
        /// 각 리스너에 대한 상대 특성입니다.
        /// </summary>
        public DSP3DAttributes[] relative = relative;

        /// <summary>
        /// The weighting applied to each listener.<br/>
        /// 각 리스너에 적용할 가중치입니다.
        /// </summary>
        public float[] listenerWeights = listenerWeights;

        /// <summary>
        /// The absolute sound attributes.<br/>
        /// 절대 사운드 특성입니다.
        /// </summary>
        public DSP3DAttributes absolute = absolute;
    }

    /// <summary>
    /// Stores the distance range over which a DSP attenuates sound.<br/>
    /// DSP가 사운드를 감쇠하는 거리 범위를 저장합니다.
    /// </summary>
    public struct AttenuationRange(float minimum, float maximum)
    {
        /// <summary>
        /// The minimum attenuation distance.<br/>
        /// 최소 감쇠 거리입니다.
        /// </summary>
        public float minimum = minimum;

        /// <summary>
        /// The maximum attenuation distance.<br/>
        /// 최대 감쇠 거리입니다.
        /// </summary>
        public float maximum = maximum;
    }

    /// <summary>
    /// Stores loudness-meter channel weighting values.<br/>
    /// 라우드니스 미터 채널 가중치 값을 저장합니다.
    /// </summary>
    public struct LoudnessMeterWeighting(float[] channelWeights)
    {
        /// <summary>
        /// The weighting value for each channel.<br/>
        /// 각 채널의 가중치 값입니다.
        /// </summary>
        public float[] channelWeights = channelWeights;
    }

    /// <summary>
    /// Stores a loudness-meter measurement snapshot.<br/>
    /// 라우드니스 미터 측정 스냅샷을 저장합니다.
    /// </summary>
    public struct LoudnessMeterInfo(
        float momentaryLoudness,
        float shortTermLoudness,
        float integratedLoudness,
        float loudness10thPercentile,
        float loudness95thPercentile,
        float[] loudnessHistogram,
        float maxTruePeak,
        float maxMomentaryLoudness)
    {
        /// <summary>
        /// The loudness measured over the current 400-millisecond window.<br/>
        /// 현재 400밀리초 창에서 측정한 라우드니스입니다.
        /// </summary>
        public float momentaryLoudness = momentaryLoudness;

        /// <summary>
        /// The loudness measured over the current 3-second window.<br/>
        /// 현재 3초 창에서 측정한 라우드니스입니다.
        /// </summary>
        public float shortTermLoudness = shortTermLoudness;

        /// <summary>
        /// The loudness measured across the recording period.<br/>
        /// 기록 기간 전체에서 측정한 라우드니스입니다.
        /// </summary>
        public float integratedLoudness = integratedLoudness;

        /// <summary>
        /// The 10th-percentile short-term loudness.<br/>
        /// 단기 라우드니스의 10백분위 값입니다.
        /// </summary>
        public float loudness10thPercentile = loudness10thPercentile;

        /// <summary>
        /// The 95th-percentile short-term loudness.<br/>
        /// 단기 라우드니스의 95백분위 값입니다.
        /// </summary>
        public float loudness95thPercentile = loudness95thPercentile;

        /// <summary>
        /// The distribution of momentary loudness values.<br/>
        /// 순간 라우드니스 값의 분포입니다.
        /// </summary>
        public float[] loudnessHistogram = loudnessHistogram;

        /// <summary>
        /// The highest true peak.<br/>
        /// 최고 트루 피크입니다.
        /// </summary>
        public float maxTruePeak = maxTruePeak;

        /// <summary>
        /// The highest momentary loudness.<br/>
        /// 최고 순간 라우드니스입니다.
        /// </summary>
        public float maxMomentaryLoudness = maxMomentaryLoudness;
    }
}
