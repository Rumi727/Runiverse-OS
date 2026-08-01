#nullable enable
using System.Diagnostics;
using System.Threading;

namespace RuniOS.Sounds
{
    public abstract class RuniAudioSource : MonoBehaviour, IAudioSource
    {
        /// <remarks>
        /// Coordinates access to active, playback, pause, and interpolated-time state.<br/>
        /// Recursion is enabled because write-locked paths invoke other members that acquire this lock again.
        /// <br/><br/>
        /// 활성, 재생, 일시 정지 및 보간 시간 상태에 대한 접근을 조정합니다.<br/>
        /// 쓰기 잠금 경로에서 이 잠금을 다시 획득하는 다른 멤버를 호출하므로 재귀 잠금이 활성화되어 있습니다.
        /// </remarks>
        public readonly ReaderWriterLockSlim playingLock = new(LockRecursionPolicy.SupportsRecursion);

        /// <remarks>
        /// The getter is thread-safe and may be called from any thread.<br/>
        /// It acquires the read lock of <see cref="playingLock"/> while reading the cached active state.
        /// <br/><br/>
        /// getter는 thread-safe하며 어떤 스레드에서든 호출할 수 있습니다.<br/>
        /// 캐시된 활성 상태를 읽는 동안 <see cref="playingLock"/>의 읽기 잠금을 획득합니다.
        /// </remarks>
        public new bool isActiveAndEnabled
        {
            get
            {
                playingLock.EnterReadLock();

                try
                {
                    return _isActiveAndEnabled;
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
        }
        bool _isActiveAndEnabled;

        /// <remarks>
        /// The getter does not acquire <see cref="playingLock"/> and returns a <see langword="volatile"/> snapshot.<br/>
        /// getter는 <see cref="playingLock"/>을 획득하지 않고 <see langword="volatile"/> 스냅샷을 반환합니다.
        /// </remarks>
        public bool isPlaying => Volatile.Read(ref _isPlaying);
        bool _isPlaying = false;

        /// <remarks>
        /// The getter does not acquire <see cref="playingLock"/> and returns a <see langword="volatile"/> snapshot.<br/>
        /// The setter invokes <see cref="Pause"/> or <see cref="UnPause"/>, which acquires the write lock.
        /// <br/><br/>
        /// getter는 <see cref="playingLock"/>을 획득하지 않고 <see langword="volatile"/> 스냅샷을 반환합니다.<br/>
        /// setter는 쓰기 잠금을 획득하는 <see cref="Pause"/> 또는 <see cref="UnPause"/>를 호출합니다.
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

        /// <remarks>
        /// The getter acquires the read lock of <see cref="playingLock"/>.<br/>
        /// The setter synchronizes the interpolated playback time while holding the write lock.
        /// <br/><br/>
        /// getter는 <see cref="playingLock"/>의 읽기 잠금을 획득합니다.<br/>
        /// setter는 쓰기 잠금을 보유한 상태에서 보간된 재생 시간을 동기화합니다.
        /// </remarks>
        public virtual double time
        {
            get
            {
                playingLock.EnterReadLock();

                try
                {
                    if (_isPlaying && !_isPaused)
                        return baseTime + (((Stopwatch.GetTimestamp() - baseTimestamp) / (double)Stopwatch.Frequency) * tempo);
                    else
                        return baseTime;
                }
                finally
                {
                    playingLock.ExitReadLock();
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

        /// <remarks>
        /// The getter does not acquire <see cref="playingLock"/> and returns the <see langword="volatile"/> value.<br/>
        /// The setter acquires the write lock, synchronizes the interpolated playback time, and then changes the tempo.
        /// <br/><br/>
        /// getter는 <see cref="playingLock"/>을 획득하지 않고 <see langword="volatile"/> 값을 반환합니다.<br/>
        /// setter는 쓰기 잠금을 획득하여 보간된 재생 시간을 동기화한 후 템포를 변경합니다.
        /// </remarks>
        public virtual float tempo
        {
            get => _tempo;
            set
            {
                playingLock.EnterWriteLock();

                try
                {
                    SyncInterpolatedTime(time);
                    _tempo = value;
                }
                finally
                {
                    playingLock.ExitWriteLock();
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
        [SerializeField] double _loopStart;

        public virtual double loopEnd
        {
            get => Volatile.Read(ref _loopEnd);
            set => Volatile.Write(ref _loopEnd, value);
        }
        [SerializeField] double _loopEnd = double.MaxValue;

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
        [SerializeField] volatile float _spread = 90;

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

        /// <remarks>
        /// Returns without acquiring the write lock when this source is inactive.<br/>
        /// Otherwise, acquires the write lock of <see cref="playingLock"/> while updating the playback state and invoking <see cref="OnPlay"/>.
        /// <br/><br/>
        /// 이 소스가 비활성 상태이면 쓰기 잠금을 획득하지 않고 반환합니다.<br/>
        /// 그 외에는 재생 상태를 갱신하고 <see cref="OnPlay"/>를 호출하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        public void Play(double startTime = 0)
        {
            if (!isActiveAndEnabled)
                return;

            playingLock.EnterWriteLock();

            try
            {
                _isPlaying = true;

                time = startTime;
                OnPlay();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnPlay() { }

        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> while updating the playback state and invoking <see cref="OnStop"/>.<br/>
        /// 재생 상태를 갱신하고 <see cref="OnStop"/>을 호출하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        public void Stop()
        {
            playingLock.EnterWriteLock();

            try
            {
                _isPlaying = false;

                time = 0;
                OnStop();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnStop() { }

        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> while updating the pause state and invoking <see cref="OnPause"/>.<br/>
        /// 일시 정지 상태를 갱신하고 <see cref="OnPause"/>를 호출하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        public void Pause()
        {
            playingLock.EnterWriteLock();

            try
            {
                SyncInterpolatedTime(time);
                _isPaused = true;

                OnPause();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnPause() { }

        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> while updating the pause state and invoking <see cref="OnUnPause"/>.<br/>
        /// 일시 정지 상태를 갱신하고 <see cref="OnUnPause"/>를 호출하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        public void UnPause()
        {
            playingLock.EnterWriteLock();

            try
            {
                SyncInterpolatedTime(time);
                _isPaused = false;

                OnUnPause();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnUnPause() { }

        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> while synchronizing the interpolated playback time.<br/>
        /// It may be called without a lock, with an upgradeable read lock, or with a write lock, but not while the current thread holds only a read lock.
        /// <br/><br/>
        /// 보간된 재생 시간을 동기화하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.<br/>
        /// 잠금을 보유하지 않거나 업그레이드 가능 읽기 잠금 또는 쓰기 잠금을 보유한 상태에서 호출할 수 있지만, 현재 스레드가 읽기 잠금만 보유한 상태에서는 호출하면 안 됩니다.
        /// </remarks>
        protected void SyncInterpolatedTime(double time)
        {
            playingLock.EnterWriteLock();

            try
            {
                baseTime = time;
                baseTimestamp = Stopwatch.GetTimestamp();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> while marking this source as active.<br/>
        /// 이 소스를 활성 상태로 표시하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        protected virtual void OnEnable()
        {
            playingLock.EnterWriteLock();

            try
            {
                _isActiveAndEnabled = true;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> while marking this source as inactive and stopping playback.<br/>
        /// 이 소스를 비활성 상태로 표시하고 재생을 정지하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        protected virtual void OnDisable()
        {
            playingLock.EnterWriteLock();

            try
            {
                _isActiveAndEnabled = false;
                Stop();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }
    }
}
