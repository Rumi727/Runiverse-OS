#nullable enable
using System.Diagnostics;
using System.Threading;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Provides the playback state, the interpolated playback time, and the playback settings shared by audio sources.<br/>
    /// 오디오 소스가 공유하는 재생 상태, 보간된 재생 시간 및 재생 설정을 제공합니다.
    /// </summary>
    public abstract class RuniAudioSource : MonoBehaviour, IPlayControl, IPausable, IStoppable, ISeekable, ILoopControl, ITempoControl, IPitchControl, IAudioEmitter
    {
        /// <summary>
        /// Gets the lock that coordinates the playback state of this source.<br/>
        /// 이 소스의 재생 상태를 조정하는 잠금을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Coordinates access to active, playback, pause, and interpolated-time state.<br/>
        /// Recursion is enabled because write-locked paths invoke other members that acquire this lock again.
        /// <br/><br/>
        /// 활성, 재생, 일시 정지 및 보간 시간 상태에 대한 접근을 조정합니다.<br/>
        /// 쓰기 잠금 경로에서 이 잠금을 다시 획득하는 다른 멤버를 호출하므로 재귀 잠금이 활성화되어 있습니다.
        /// </remarks>
        public readonly ReaderWriterLockSlim playingLock = new(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Gets whether this source is active and enabled, as cached when it was last enabled or disabled.<br/>
        /// 마지막으로 활성화되거나 비활성화될 때 캐시된, 이 소스가 활성화되어 있는지 여부를 가져옵니다.
        /// </summary>
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

        /// <summary>
        /// Gets whether this source is playing, which stays <see langword="true"/> while it is paused.<br/>
        /// 이 소스가 재생 중인지 여부를 가져오며, 일시 정지 중에도 <see langword="true"/>로 유지됩니다.
        /// </summary>
        /// <remarks>
        /// The getter does not acquire <see cref="playingLock"/> and returns a <see langword="volatile"/> snapshot.<br/>
        /// getter는 <see cref="playingLock"/>을 획득하지 않고 <see langword="volatile"/> 스냅샷을 반환합니다.
        /// </remarks>
        public bool isPlaying => Volatile.Read(ref _isPlaying);
        bool _isPlaying = false;

        /// <summary>
        /// Gets or sets whether the playback is paused.<br/>
        /// 재생이 일시 정지되었는지 여부를 가져오거나 설정합니다.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the playback position in seconds, interpolated from the last synchronization at <see cref="tempo"/>.<br/>
        /// 마지막 동기화로부터 <see cref="tempo"/>로 보간된 초 단위 재생 위치를 가져오거나 설정합니다.
        /// </summary>
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
                    // The time holds until a base timestamp in the future is reached.
                    if (_isPlaying && !_isPaused)
                        return baseTime + (((Stopwatch.GetTimestamp() - baseTimestamp).Clamp(0) / (double)Stopwatch.Frequency) * tempo);
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

        /// <summary>
        /// Gets the playback time that is being output now, as opposed to <see cref="time"/>, which is where the playback is.<br/>
        /// <see cref="time"/>이 재생 위치인 것과 달리, 지금 출력되고 있는 재생 시간을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// The derived source synchronizes it independently of <see cref="time"/> from what its backend is outputting, such as behind an output buffer.<br/>
        /// It may differ from what is heard right after an explicit move, such as seeking, looping, or pausing.<br/>
        /// A source that does not synchronize it only keeps it continuous across playing, pausing, and tempo changes.<br/>
        /// The getter acquires the read lock of <see cref="playingLock"/>.
        /// <br/><br/>
        /// 하위 소스는 출력 버퍼 뒤처럼 백엔드가 출력하고 있는 위치를 바탕으로 <see cref="time"/>과 독립적으로 이 값을 동기화합니다.<br/>
        /// 탐색, 반복, 일시 정지처럼 명시적으로 이동한 직후에는 들리는 위치와 다를 수 있습니다.<br/>
        /// 이 값을 동기화하지 않는 소스는 재생, 일시 정지 및 템포 변경에 걸쳐 값을 이어 주기만 합니다.<br/>
        /// getter는 <see cref="playingLock"/>의 읽기 잠금을 획득합니다.
        /// </remarks>
        public virtual double outputTime
        {
            get
            {
                playingLock.EnterReadLock();

                try
                {
                    return GetInterpolatedOutputTimeUnsafe(Stopwatch.GetTimestamp());
                }
                finally
                {
                    playingLock.ExitReadLock();
                }
            }
        }
        double outputBaseTime = 0;
        long outputBaseTimestamp = 0;

        /// <summary>
        /// Gets the length of the current clip in seconds.<br/>
        /// 현재 클립의 길이를 초 단위로 가져옵니다.
        /// </summary>
        public abstract double length { get; }

        /// <summary>
        /// Gets or sets the volume of this source.<br/>
        /// 이 소스의 볼륨을 가져오거나 설정합니다.
        /// </summary>
        public virtual float volume
        {
            get => _volume;
            set => _volume = value;
        }
        [SerializeField] volatile float _volume = 1;

        /// <summary>
        /// Gets or sets the playback speed multiplier, which also scales how fast <see cref="time"/> advances.<br/>
        /// 재생 속도 배율을 가져오거나 설정하며, 이 값은 <see cref="time"/>이 진행하는 속도에도 곱해집니다.
        /// </summary>
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
                    ContinueOutputTimeUnsafe();
                    _tempo = value;
                }
                finally
                {
                    playingLock.ExitWriteLock();
                }
            }
        }
        [SerializeField] volatile float _tempo = 1;

        /// <summary>
        /// Gets or sets the pitch multiplier of this source.<br/>
        /// 이 소스의 피치 배율을 가져오거나 설정합니다.
        /// </summary>
        public virtual float pitch
        {
            get => _pitch;
            set => _pitch = value;
        }
        [SerializeField] volatile float _pitch = 1;

        /// <summary>
        /// Gets or sets whether the playback loops between <see cref="loopStart"/> and <see cref="loopEnd"/>.<br/>
        /// 재생이 <see cref="loopStart"/>와 <see cref="loopEnd"/> 사이에서 반복되는지 여부를 가져오거나 설정합니다.
        /// </summary>
        public virtual bool loop
        {
            get => _loop;
            set => _loop = value;
        }
        [SerializeField] volatile bool _loop = false;

        /// <summary>
        /// Gets or sets the loop start position in seconds.<br/>
        /// 반복 시작 위치를 초 단위로 가져오거나 설정합니다.
        /// </summary>
        public virtual double loopStart
        {
            get => Volatile.Read(ref _loopStart);
            set => Volatile.Write(ref _loopStart, value);
        }
        [SerializeField] double _loopStart;

        /// <summary>
        /// Gets or sets the loop end position in seconds.<br/>
        /// 반복 종료 위치를 초 단위로 가져오거나 설정합니다.
        /// </summary>
        public virtual double loopEnd
        {
            get => Volatile.Read(ref _loopEnd);
            set => Volatile.Write(ref _loopEnd, value);
        }
        [SerializeField] double _loopEnd = double.MaxValue;

        /// <summary>
        /// Gets or sets the left/right pan level.<br/>
        /// 좌우 팬 레벨을 가져오거나 설정합니다.
        /// </summary>
        public virtual float panStereo
        {
            get => _panStereo;
            set => _panStereo = value;
        }
        [SerializeField] volatile float _panStereo = 0;

        /// <summary>
        /// Gets or sets the blend between 2D and 3D spatialization.<br/>
        /// 2D와 3D 공간화 사이의 블렌드를 가져오거나 설정합니다.
        /// </summary>
        public virtual float spatialBlend
        {
            get => _spatialBlend;
            set => _spatialBlend = value;
        }
        [SerializeField] volatile float _spatialBlend = 0;

        /// <summary>
        /// Gets or sets the Doppler effect level.<br/>
        /// 도플러 효과 수준을 가져오거나 설정합니다.
        /// </summary>
        public virtual float dopplerLevel
        {
            get => _dopplerLevel;
            set => _dopplerLevel = value;
        }
        [SerializeField] volatile float _dopplerLevel = 1;

        /// <summary>
        /// Gets or sets the stereo spread angle in degrees.<br/>
        /// 스테레오 확산 각도를 도 단위로 가져오거나 설정합니다.
        /// </summary>
        public virtual float spread
        {
            get => _spread;
            set => _spread = value;
        }
        [SerializeField] volatile float _spread = 0;

        /// <summary>
        /// Gets or sets the minimum distance for 3D attenuation.<br/>
        /// 3D 감쇠에 사용할 최소 거리를 가져오거나 설정합니다.
        /// </summary>
        public virtual float minDistance
        {
            get => _minDistance;
            set => _minDistance = value;
        }
        [SerializeField] volatile float _minDistance = 0;

        /// <summary>
        /// Gets or sets the maximum distance for 3D attenuation.<br/>
        /// 3D 감쇠에 사용할 최대 거리를 가져오거나 설정합니다.
        /// </summary>
        public virtual float maxDistance
        {
            get => _maxDistance;
            set => _maxDistance = value;
        }
        [SerializeField] volatile float _maxDistance = 16;

        /// <summary>
        /// Starts the playback from <paramref name="startTime"/>.<br/>
        /// <paramref name="startTime"/>부터 재생을 시작합니다.
        /// </summary>
        /// <param name="startTime">
        /// The playback position in seconds to start from.<br/>
        /// 재생을 시작할 초 단위 위치입니다.
        /// </param>
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
                ContinueOutputTimeUnsafe();
                _isPlaying = true;

                time = startTime;
                OnPlay();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Called after the playback has started and <see cref="time"/> has been set to the start position.<br/>
        /// 재생이 시작되고 <see cref="time"/>이 시작 위치로 설정된 후 호출됩니다.
        /// </summary>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnPlay() { }

        /// <summary>
        /// Stops the playback and moves <see cref="time"/> and <see cref="outputTime"/> to zero.<br/>
        /// 재생을 정지하고 <see cref="time"/>과 <see cref="outputTime"/>을 0으로 옮깁니다.
        /// </summary>
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
                SyncInterpolatedOutputTime(0, Stopwatch.GetTimestamp());
                OnStop();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Called after the playback has stopped and <see cref="time"/> has been set to zero.<br/>
        /// 재생이 정지되고 <see cref="time"/>이 0으로 설정된 후 호출됩니다.
        /// </summary>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnStop() { }

        /// <summary>
        /// Pauses the playback at the current position.<br/>
        /// 현재 위치에서 재생을 일시 정지합니다.
        /// </summary>
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
                ContinueOutputTimeUnsafe();
                _isPaused = true;

                OnPause();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Called after <see cref="isPaused"/> has become <see langword="true"/>.<br/>
        /// <see cref="isPaused"/>가 <see langword="true"/>가 된 후 호출됩니다.
        /// </summary>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnPause() { }

        /// <summary>
        /// Resumes the paused playback from the current position.<br/>
        /// 일시 정지된 재생을 현재 위치에서 다시 시작합니다.
        /// </summary>
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
                ContinueOutputTimeUnsafe();
                _isPaused = false;

                OnUnPause();
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Called after <see cref="isPaused"/> has become <see langword="false"/>.<br/>
        /// <see cref="isPaused"/>가 <see langword="false"/>가 된 후 호출됩니다.
        /// </summary>
        /// <remarks>
        /// Called while the current thread holds the write lock of <see cref="playingLock"/>.<br/>
        /// 현재 스레드가 <see cref="playingLock"/>의 쓰기 잠금을 보유한 상태에서 호출됩니다.
        /// </remarks>
        protected virtual void OnUnPause() { }

        /// <summary>
        /// Synchronizes the interpolated playback time so that it is <paramref name="time"/> now.<br/>
        /// 보간된 재생 시간이 지금 <paramref name="time"/>이 되도록 동기화합니다.
        /// </summary>
        /// <param name="time">
        /// The playback time now.<br/>
        /// 지금의 재생 시간입니다.
        /// </param>
        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> while synchronizing the interpolated playback time.<br/>
        /// It may be called without a lock, with an upgradeable read lock, or with a write lock, but not while the current thread holds only a read lock.
        /// <br/><br/>
        /// 보간된 재생 시간을 동기화하는 동안 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.<br/>
        /// 잠금을 보유하지 않거나 업그레이드 가능 읽기 잠금 또는 쓰기 잠금을 보유한 상태에서 호출할 수 있지만, 현재 스레드가 읽기 잠금만 보유한 상태에서는 호출하면 안 됩니다.
        /// </remarks>
        protected void SyncInterpolatedTime(double time) => SyncInterpolatedTime(time, Stopwatch.GetTimestamp());

        /// <summary>
        /// Synchronizes the interpolated playback time so that it was <paramref name="time"/> at <paramref name="timestamp"/>.<br/>
        /// 보간된 재생 시간이 <paramref name="timestamp"/>에 <paramref name="time"/>이었도록 동기화합니다.
        /// </summary>
        /// <param name="time">
        /// The playback time at <paramref name="timestamp"/>.<br/>
        /// <paramref name="timestamp"/>에서의 재생 시간입니다.
        /// </param>
        /// <param name="timestamp">
        /// The <see cref="Stopwatch.GetTimestamp"/> value at which the playback was at <paramref name="time"/>.<br/>
        /// A timestamp in the future holds the time at <paramref name="time"/> until it is reached.
        /// <br/><br/>
        /// 재생 위치가 <paramref name="time"/>이었던 <see cref="Stopwatch.GetTimestamp"/> 값입니다.<br/>
        /// 미래의 타임스탬프는 그 시각이 될 때까지 시간을 <paramref name="time"/>에 유지합니다.
        /// </param>
        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> under the same conditions as <see cref="SyncInterpolatedTime(double)"/>.<br/>
        /// <see cref="SyncInterpolatedTime(double)"/>과 같은 조건으로 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        protected void SyncInterpolatedTime(double time, long timestamp)
        {
            playingLock.EnterWriteLock();

            try
            {
                baseTime = time;
                baseTimestamp = timestamp;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        /// <summary>
        /// Synchronizes the interpolated output time so that it was <paramref name="time"/> at <paramref name="timestamp"/>.<br/>
        /// 보간된 출력 시간이 <paramref name="timestamp"/>에 <paramref name="time"/>이었도록 동기화합니다.
        /// </summary>
        /// <param name="time">
        /// The playback time being output at <paramref name="timestamp"/>.<br/>
        /// <paramref name="timestamp"/>에 출력되던 재생 시간입니다.
        /// </param>
        /// <param name="timestamp">
        /// The <see cref="Stopwatch.GetTimestamp"/> value at which <paramref name="time"/> was being output.<br/>
        /// A timestamp in the future holds the output time at <paramref name="time"/> until it is reached.
        /// <br/><br/>
        /// <paramref name="time"/>이 출력되던 <see cref="Stopwatch.GetTimestamp"/> 값입니다.<br/>
        /// 미래의 타임스탬프는 그 시각이 될 때까지 출력 시간을 <paramref name="time"/>에 유지합니다.
        /// </param>
        /// <remarks>
        /// Acquires the write lock of <see cref="playingLock"/> under the same conditions as <see cref="SyncInterpolatedTime(double)"/>.<br/>
        /// <see cref="SyncInterpolatedTime(double)"/>과 같은 조건으로 <see cref="playingLock"/>의 쓰기 잠금을 획득합니다.
        /// </remarks>
        protected void SyncInterpolatedOutputTime(double time, long timestamp)
        {
            playingLock.EnterWriteLock();

            try
            {
                outputBaseTime = time;
                outputBaseTimestamp = timestamp;
            }
            finally
            {
                playingLock.ExitWriteLock();
            }
        }

        // The output time holds until a base timestamp in the future is reached, like the time.
        double GetInterpolatedOutputTimeUnsafe(long timestamp)
        {
            if (_isPlaying && !_isPaused)
                return outputBaseTime + (((timestamp - outputBaseTimestamp).Clamp(0) / (double)Stopwatch.Frequency) * tempo);
            else
                return outputBaseTime;
        }

        /// <summary>
        /// Rebases the output time on its current value before the playback state or the tempo changes, so that it does not jump.<br/>
        /// A base timestamp in the future is kept, so the output time still holds until it.<br/>
        /// The caller holds the write lock of <see cref="playingLock"/>.
        /// <br/><br/>
        /// 재생 상태나 템포가 바뀌기 전에 출력 시간을 현재 값으로 다시 기준 잡아 값이 튀지 않게 합니다.<br/>
        /// 미래의 기준 타임스탬프는 유지하므로, 출력 시간은 여전히 그 시각까지 유지됩니다.<br/>
        /// 호출자는 <see cref="playingLock"/>의 쓰기 잠금을 보유합니다.
        /// </summary>
        void ContinueOutputTimeUnsafe()
        {
            long timestamp = Max(Stopwatch.GetTimestamp(), outputBaseTimestamp);
            outputBaseTime = GetInterpolatedOutputTimeUnsafe(timestamp);
            outputBaseTimestamp = timestamp;
        }

        /// <summary>
        /// Marks this source as active when the component is enabled.<br/>
        /// 컴포넌트가 활성화되면 이 소스를 활성 상태로 표시합니다.
        /// </summary>
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

        /// <summary>
        /// Marks this source as inactive and stops the playback when the component is disabled.<br/>
        /// 컴포넌트가 비활성화되면 이 소스를 비활성 상태로 표시하고 재생을 정지합니다.
        /// </summary>
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
