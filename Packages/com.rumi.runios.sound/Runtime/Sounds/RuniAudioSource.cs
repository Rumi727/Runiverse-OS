#nullable enable
using System.Diagnostics;
using System.Threading;

namespace RuniOS.Sounds
{
    public abstract class RuniAudioSource : MonoBehaviour, IAudioSource
    {
        /// <remarks>
        /// <see cref="isPlaying"/>, <see cref="time"/>, <see cref="Play"/> 등 플레이 상태에 영향을 주는 멤버와 관련된 잠금 오브젝트입니다.
        /// </remarks>
        public readonly object playingLock = new object();

        public new bool isActiveAndEnabled
        {
            get
            {
                lock (playingLock)
                    return _isActiveAndEnabled;
            }
        }
        bool _isActiveAndEnabled;

        /// <remarks>
        /// <see cref="playingLock"/> 없이 호출 시점의 값을 가져옵니다.
        /// </remarks>
        public bool isPlaying => Volatile.Read(ref _isPlaying);
        bool _isPlaying = false;

        /// <remarks>
        /// <see cref="playingLock"/> 없이 호출 시점의 값을 가져옵니다.
        /// </remarks>
        public bool isPaused
        {
            get => Volatile.Read(ref _isPaused);
            set
            {
                if (value)
                    Pause();
                else
                    UnPause();
            }
        }
        bool _isPaused = false;

        public virtual double time
        {
            get
            {
                lock (playingLock)
                {
                    if (_isPlaying && !_isPaused)
                        return baseTime + (((Stopwatch.GetTimestamp() - baseTimestamp) / (double)Stopwatch.Frequency) * tempo);
                    else
                        return baseTime;
                }
            }
            set => SyncInterpolatedTime(value);
        }
        double baseTime = 0;
        long baseTimestamp = 0;

        public abstract double length { get; }

        public virtual float volume
        {
            get => _volume;
            set => _volume = value;
        }
        [SerializeField] volatile float _volume = 1;

        public virtual float tempo
        {
            get => _tempo;
            set
            {
                lock (playingLock)
                {
                    SyncInterpolatedTime(time);
                    _tempo = value;
                }
            }
        }
        [SerializeField] volatile float _tempo = 1;

        public virtual float pitch
        {
            get => _pitch;
            set => _pitch = value;
        }
        [SerializeField] volatile float _pitch = 1;

        public virtual bool loop
        {
            get => _loop;
            set => _loop = value;
        }
        [SerializeField] volatile bool _loop = false;

        public virtual double loopStart
        {
            get => Volatile.Read(ref _loopStart);
            set => Volatile.Write(ref _loopStart, value);
        }
        double _loopStart;

        public virtual double loopEnd
        {
            get => Volatile.Read(ref _loopEnd);
            set => Volatile.Write(ref _loopEnd, value);
        }
        double _loopEnd = double.MaxValue;

        public virtual float panStereo
        {
            get => _panStereo;
            set => _panStereo = value;
        }
        [SerializeField] volatile float _panStereo = 0;

        public virtual float spatialBlend
        {
            get => _spatialBlend;
            set => _spatialBlend = value;
        }
        [SerializeField] volatile float _spatialBlend = 0;

        public virtual float dopplerLevel
        {
            get => _dopplerLevel;
            set => _dopplerLevel = value;
        }
        [SerializeField] volatile float _dopplerLevel = 0;

        public virtual float spread
        {
            get => _spread;
            set => _spread = value;
        }
        [SerializeField] volatile float _spread = 45;

        public virtual float minDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }
        [SerializeField] volatile float _minDistance = 0;

        public virtual float maxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }
        [SerializeField] volatile float _maxDistance = 16;

        bool IAudioPlayer.isPitchSupported => true;



        public void Play(double startTime = 0)
        {
            if (!isActiveAndEnabled)
                return;

            lock (playingLock)
            {
                _isPlaying = true;

                time = startTime;
                OnPlay();
            }
        }

        protected virtual void OnPlay() { }

        public void Stop()
        {
            lock (playingLock)
            {
                _isPlaying = false;

                time = 0;
                OnStop();
            }
        }

        protected virtual void OnStop() { }

        public void Pause()
        {
            lock (playingLock)
            {
                SyncInterpolatedTime(time);
                _isPaused = true;

                OnPause();
            }
        }

        protected virtual void OnPause() { }

        public void UnPause()
        {
            lock (playingLock)
            {
                SyncInterpolatedTime(time);
                _isPaused = false;

                OnUnPause();
            }
        }

        protected virtual void OnUnPause() { }

        protected void SyncInterpolatedTime(double time)
        {
            lock (playingLock)
            {
                baseTime = time;
                baseTimestamp = Stopwatch.GetTimestamp();
            }
        }

        protected virtual void OnEnable()
        {
            lock (playingLock)
                _isActiveAndEnabled = true;
        }

        protected virtual void OnDisable()
        {
            lock (playingLock)
            {
                _isActiveAndEnabled = false;
                Stop();
            }
        }
    }
}
