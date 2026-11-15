#nullable enable
using RuniOS.Sounds;

namespace RuniOS.NBS
{
    public sealed partial class NBSVoiceScheduler<TTransport>
    {
        /// <summary>
        /// Gets or sets the volume multiplier applied to every sound, including the ones already playing.<br/>
        /// 이미 재생 중인 소리를 포함한 모든 소리에 적용할 음량 배율을 가져오거나 설정합니다.
        /// </summary>
        public float volume
        {
            get => _volume;
            set => SetMix(ref _volume, value, static (scheduler, voice) => voice.channel.volume = scheduler.timeline.GetMix(voice.mapIndex).volume * scheduler._volume);
        }
        float _volume = 1;

        /// <summary>
        /// Gets or sets the stereo position from <c>-1</c> to <c>1</c> blended with the stereo position of each note.<br/>
        /// 각 노트의 스테레오 위치와 섞을 <c>-1</c>부터 <c>1</c>까지의 스테레오 위치를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// The result is <c>Lerp(notePanning, panStereo, |panStereo|)</c>.<br/>
        /// 결과는 <c>Lerp(notePanning, panStereo, |panStereo|)</c>입니다.
        /// </remarks>
        public float panStereo
        {
            get => _panStereo;
            set => SetMix(ref _panStereo, value, static (scheduler, voice) => voice.channel.panStereo = scheduler.GetPan(voice));
        }
        float _panStereo;

        /// <summary>
        /// Gets or sets the blend between 2D and 3D sound, applied to every sound.<br/>
        /// 모든 소리에 적용할 2D와 3D 사운드 사이의 혼합 정도를 가져오거나 설정합니다.
        /// </summary>
        public float spatialBlend
        {
            get => _spatialBlend;
            set => SetMix(ref _spatialBlend, value, static (scheduler, voice) =>
            {
                voice.channel.spatialBlend = scheduler._spatialBlend;
                voice.channel.spatialState = scheduler._spatialState;
            });
        }
        float _spatialBlend;

        /// <summary>
        /// Gets or sets the Doppler scale, applied to every sound.<br/>
        /// 모든 소리에 적용할 도플러 배율을 가져오거나 설정합니다.
        /// </summary>
        public float dopplerLevel
        {
            get => _dopplerLevel;
            set => SetMix(ref _dopplerLevel, value, static (scheduler, voice) => voice.channel.dopplerLevel = scheduler._dopplerLevel);
        }
        float _dopplerLevel = 1;

        /// <summary>
        /// Gets or sets the spread angle in degrees, applied to every sound.<br/>
        /// 모든 소리에 적용할 퍼짐 각도를 도 단위로 가져오거나 설정합니다.
        /// </summary>
        public float spread
        {
            get => _spread;
            set => SetMix(ref _spread, value, static (scheduler, voice) => voice.channel.spread = scheduler._spread);
        }
        float _spread;

        /// <summary>
        /// Gets or sets the minimum and maximum 3D distances, applied to every sound.<br/>
        /// 모든 소리에 적용할 최소 및 최대 3D 거리를 가져오거나 설정합니다.
        /// </summary>
        public (float min, float max) minMaxDistance
        {
            get => _minMaxDistance;
            set => SetMix(ref _minMaxDistance, value, static (scheduler, voice) => voice.channel.minMaxDistance = scheduler._minMaxDistance);
        }
        (float min, float max) _minMaxDistance = (1, 500);

        /// <summary>
        /// Gets or sets the distance rolloff, applied to every sound.<br/>
        /// 모든 소리에 적용할 거리 감쇠 방식을 가져오거나 설정합니다.
        /// </summary>
        public SoundRolloffMode rolloffMode
        {
            get => _rolloffMode;
            set => SetMix(ref _rolloffMode, value, static (scheduler, voice) => voice.channel.rolloffMode = scheduler._rolloffMode);
        }
        SoundRolloffMode _rolloffMode = SoundRolloffMode.inverse;

        /// <summary>
        /// Gets or sets the 3D position and velocity of the emitter, applied to every sound.<br/>
        /// 모든 소리에 적용할 발신원의 3D 위치와 속도를 가져오거나 설정합니다.
        /// </summary>
        public AudioSpatialState spatialState
        {
            get => _spatialState;
            set
            {
                // AudioSpatialState has no IEquatable, so it is compared here instead of boxing every frame.
                if (value.position == _spatialState.position && value.velocity == _spatialState.velocity && value.rotation == _spatialState.rotation)
                    return;

                SetMix(ref _spatialState, value, static (scheduler, voice) =>
                {
                    // A fully 2D sound ignores its 3D attributes; they are applied once the blend changes.
                    if (scheduler._spatialBlend != 0)
                        voice.channel.spatialState = scheduler._spatialState;
                }, false);
            }
        }
        AudioSpatialState _spatialState;

        void SetMix<T>(ref T field, T value, Action<NBSVoiceScheduler<TTransport>, Voice> apply, bool compare = true)
        {
            lock (gate)
            {
                if (compare && EqualityComparer<T>.Default.Equals(field, value))
                    return;

                field = value;

                List<Voice> snapshot = SnapshotVoicesUnsafe();
                for (int i = 0; i < snapshot.Count; i++)
                {
                    try
                    {
                        apply(this, snapshot[i]);
                    }
                    catch (ObjectDisposedException)
                    {
                        RemoveVoice(snapshot[i].channel);
                    }
                }
            }
        }

        float GetPan(Voice voice) => Mathf.Lerp(timeline.GetMix(voice.mapIndex).panning, _panStereo, Math.Abs(_panStereo));

        void ApplyMix(Voice voice)
        {
            NBSNoteMix mix = timeline.GetMix(voice.mapIndex);
            SoundChannel channel = voice.channel;

            channel.volume = mix.volume * _volume;
            channel.panStereo = GetPan(voice);
            channel.spatialBlend = _spatialBlend;
            channel.dopplerLevel = _dopplerLevel;
            channel.spread = _spread;
            channel.minMaxDistance = _minMaxDistance;
            channel.rolloffMode = _rolloffMode;
            channel.spatialState = _spatialState;
        }
    }
}
