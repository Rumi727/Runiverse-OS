#nullable enable
using FMOD;
using System.Collections.Concurrent;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem : IDisposable
    {
        enum LifecycleState
        {
            Active,
            Resetting,
            Faulted,
            Disposed
        }

        static readonly ConcurrentDictionary<IntPtr, SoundSystem> systemLists = [];
        static readonly object systemLifetimeLock = new();

        static SoundSystem()
        {
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            const uint stackSize = 1024 * 1024;

            FMOD.Thread.SetAttributes(THREAD_TYPE.NONBLOCKING, THREAD_AFFINITY.GROUP_DEFAULT, THREAD_PRIORITY.DEFAULT, (THREAD_STACK_SIZE)stackSize).ThrowIfNotOk();
            FMOD.Thread.SetAttributes(THREAD_TYPE.FILE, THREAD_AFFINITY.GROUP_DEFAULT, THREAD_PRIORITY.DEFAULT, (THREAD_STACK_SIZE)stackSize).ThrowIfNotOk();
            FMOD.Thread.SetAttributes(THREAD_TYPE.STREAM, THREAD_AFFINITY.GROUP_DEFAULT, THREAD_PRIORITY.DEFAULT, (THREAD_STACK_SIZE)stackSize).ThrowIfNotOk();
#endif

            main = new SoundSystem(new SoundSystemSettings
            {
                softwareChannels = 1023,
                initFlags = INITFLAGS.VOL0_BECOMES_VIRTUAL
            });
        }

        /// <summary>
        /// Gets the automatically updated main system configured with 4095 virtual channels and 1024 real channels.<br/>
        /// 가상 채널 4095개와 실제 채널 1024개로 구성되어 자동으로 갱신되는 메인 시스템을 가져옵니다.
        /// </summary>
        public static SoundSystem main { get; }

        /// <summary>
        /// Initializes a new sound system with the specified pre-initialization settings.<br/>
        /// 지정된 사전 초기화 설정으로 새 사운드 시스템을 초기화합니다.
        /// </summary>
        /// <param name="settings">
        /// Settings applied before FMOD initialization. A <see langword="null"/> property uses the wrapper or FMOD default.<br/>
        /// FMOD 초기화 전에 적용할 설정입니다. <see langword="null"/>인 속성은 래퍼 또는 FMOD 기본값을 사용합니다.
        /// </param>
        /// <exception cref="FMODException">
        /// Thrown when FMOD cannot create, configure, or initialize the native system.<br/>
        /// FMOD가 네이티브 시스템을 생성하거나 설정하거나 초기화하지 못한 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// <see cref="main"/> is updated automatically. The owner of any other instance must call <see cref="Update"/> and <see cref="Dispose()"/>.<br/>
        /// <see cref="main"/>은 자동으로 갱신됩니다. 그 밖의 인스턴스는 소유자가 <see cref="Update"/>와 <see cref="Dispose()"/>를 호출해야 합니다.
        /// </remarks>
        public SoundSystem(SoundSystemSettings settings = default)
        {
            listeners = new Listeners(this);

            lock (systemLifetimeLock)
                Factory.System_Create(out native).ThrowIfNotOk();

            try
            {
                InitializeNative(settings);
                systemLists[native.handle] = this;
            }
            catch
            {
                lock (systemLifetimeLock)
                {
                    if (native.hasHandle())
                        native.release().LogErrorIfNotOk();

                    native.clearHandle();
                }

                lifecycleState = LifecycleState.Disposed;
                throw;
            }
        }

        FMOD.System native;
        readonly ReaderWriterLockSlim nativeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        readonly object lifecycleLock = new();

        bool nativeInitialized;

        readonly ConcurrentDictionary<ISoundSystemResource, byte> ownedResources = [];
        ConcurrentQueue<ISoundSystemResource>? queuedDisposals = [];

        // nativeLock 밖에서 실행 중인 리소스 해제 호출 수입니다.
        readonly object resourceDisposalLock = new();
        int activeResourceDisposals = 0;

        volatile LifecycleState lifecycleState;

        /// <summary>
        /// Gets the sample rate used by the FMOD software mixer output.<br/>
        /// FMOD 소프트웨어 믹서 출력에 사용되는 샘플 레이트를 가져옵니다.
        /// </summary>
        public int outputSampleRate => Volatile.Read(ref _outputSampleRate);
        int _outputSampleRate;

        /// <summary>
        /// Gets the maximum number of virtual channels configured for this FMOD system.<br/>
        /// 이 FMOD 시스템에 설정된 최대 가상 채널 수를 가져옵니다.
        /// </summary>
        public int maxChannels => Volatile.Read(ref _maxChannels);
        int _maxChannels;

        /// <summary>
        /// Gets the maximum number of software-mixed real channels configured for this FMOD system.<br/>
        /// 이 FMOD 시스템에 설정된 최대 소프트웨어 믹싱 실제 채널 수를 가져옵니다.
        /// </summary>
        public int softwareChannelCount => Volatile.Read(ref _softwareChannelCount);
        int _softwareChannelCount;

        /// <summary>
        /// Gets the number of virtual channels currently playing in this FMOD system.<br/>
        /// 이 FMOD 시스템에서 현재 재생 중인 가상 채널 수를 가져옵니다.
        /// </summary>
        public int playingChannelCount => UseNative(system =>
        {
            system.getChannelsPlaying(out int channels).ThrowIfNotOk();
            return channels;
        });

        /// <summary>
        /// Gets the current tail DSP clock of the master channel group in samples.<br/>
        /// 마스터 채널 그룹 tail의 현재 DSP 클록을 샘플 단위로 가져옵니다.
        /// </summary>
        /// <returns>
        /// The master channel group's current DSP clock.<br/>
        /// 마스터 채널 그룹의 현재 DSP 클록입니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system has been disposed.<br/>
        /// 이 사운드 시스템이 해제된 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when this sound system is resetting or faulted.<br/>
        /// 이 사운드 시스템이 재설정 중이거나 오류 상태인 경우 발생합니다.
        /// </exception>
        public ulong GetMasterDSPClock()
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();
                native.getMasterChannelGroup(out ChannelGroup masterGroup).ThrowIfNotOk();
                masterGroup.getDSPClock(out ulong dspClock, out _).ThrowIfNotOk();
                return dspClock;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public bool isDisposed => lifecycleState == LifecycleState.Disposed;

        public static SoundSystem GetManaged(IntPtr handle) => systemLists.GetValueOrDefault(handle);

        public void Register(ISoundSystemResource resource)
        {
            if (resource.system != this)
                throw new InvalidOperationException();

            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();
                ownedResources.TryAdd(resource, 0);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Disposes <paramref name="resource"/> immediately.<br/>
        /// <paramref name="resource"/>를 즉시 해제합니다.
        /// </summary>
        /// <param name="resource">
        /// The resource owned by this sound system to dispose.<br/>
        /// 이 사운드 시스템이 소유하며 해제할 리소스입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="resource"/> belongs to a different <see cref="SoundSystem"/>.<br/>
        /// <paramref name="resource"/>가 다른 <see cref="SoundSystem"/>에 속한 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Repeated calls are allowed. Only the first call that claims a registered resource invokes its unmanaged release; later calls are ignored.<br/>
        /// 반복 호출할 수 있습니다. 등록된 리소스를 먼저 확보한 호출만 비관리 리소스 해제를 수행하며 이후 호출은 무시됩니다.
        /// </remarks>
        public void Dispose(ISoundSystemResource resource)
        {
            if (resource.system != this)
                throw new InvalidOperationException();

            nativeLock.EnterReadLock();

            try
            {
                if (lifecycleState != LifecycleState.Active || !ownedResources.TryRemove(resource, out _))
                    return;

                lock (resourceDisposalLock)
                    activeResourceDisposals++;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }

            try
            {
                // 네이티브 잠금은 소유권 확보만 보호하며 구현 코드는 잠그지 않습니다.
                resource.ReleaseUnmanagedResources();
            }
            catch
            {
                // 일반 해제 실패는 소유권을 복구해 호출자가 원인을 정리한 뒤 재시도할 수 있게 합니다.
                ownedResources.TryAdd(resource, 0);
                throw;
            }
            finally
            {
                lock (resourceDisposalLock)
                {
                    activeResourceDisposals--;

                    if (activeResourceDisposals == 0)
                        Monitor.PulseAll(resourceDisposalLock);
                }
            }
        }

        /// <summary>
        /// Queues <paramref name="resource"/> for disposal during a later system update.<br/>
        /// <paramref name="resource"/>를 이후 시스템 업데이트에서 해제하도록 큐에 등록합니다.
        /// </summary>
        /// <param name="resource">
        /// The resource owned by this sound system to queue for disposal.<br/>
        /// 이 사운드 시스템이 소유하며 해제 큐에 등록할 리소스입니다.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when <paramref name="resource"/> belongs to a different <see cref="SoundSystem"/>.<br/>
        /// <paramref name="resource"/>가 다른 <see cref="SoundSystem"/>에 속한 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Repeated requests are allowed. The first disposal that claims the registered resource releases it; later queued or immediate requests are ignored.<br/>
        /// Requests made after this system begins resetting or disposal are ignored.<br/>
        /// 반복 요청할 수 있습니다. 등록된 리소스를 먼저 확보한 해제만 리소스를 해제하며 이후 큐 또는 즉시 요청은 무시됩니다.<br/>
        /// 이 시스템의 Reset 또는 해제가 시작된 뒤 요청은 무시됩니다.
        /// </remarks>
        public void QueueDispose(ISoundSystemResource resource)
        {
            if (resource.system != this)
                throw new InvalidOperationException();

            if (lifecycleState != LifecycleState.Active)
                return;

            ConcurrentQueue<ISoundSystemResource>? disposalQueue = Volatile.Read(ref queuedDisposals);
            disposalQueue?.Enqueue(resource);
        }

        /// <summary>
        /// Releases this sound system and all resources it still owns.<br/>
        /// 이 사운드 시스템과 여전히 소유하는 모든 리소스를 해제합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called while the current thread holds the system native lock, while this lifecycle is resetting, or from an <see cref="onResetting"/> handler.<br/>
        /// 현재 스레드가 시스템 네이티브 잠금을 보유했거나, 이 lifecycle이 Reset 중이거나, <see cref="onResetting"/> 처리기에서 호출한 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Repeated calls are ignored. Resource and native release failures are logged, and shutdown continues.<br/>
        /// 반복 호출은 무시됩니다. 리소스 및 네이티브 해제 실패는 로그로 출력하고 종료를 계속합니다.
        /// </remarks>
        public void Dispose()
        {
            ThrowIfSystemLockHeld();

            lock (lifecycleLock)
            {
                if (invokingResettingHandlers)
                    throw new InvalidOperationException("The FMOD sound system lifecycle cannot be changed from an onResetting handler.");

                if (lifecycleState == LifecycleState.Disposed)
                    return;

                List<ISoundSystemResource> resources = BeginResourceRelease(LifecycleState.Disposed);
                ReleaseOwnedResources(resources);

                nativeLock.EnterWriteLock();

                try
                {
                    lock (systemLifetimeLock)
                        native.release().LogErrorIfNotOk();

                    nativeInitialized = false;
                    native.clearHandle();
                }
                finally
                {
                    nativeLock.ExitWriteLock();
                }
            }
        }

        void InitializeNative(SoundSystemSettings settings)
        {
            if (settings.softwareChannels is { } softwareChannels)
                native.setSoftwareChannels(softwareChannels).ThrowIfNotOk();

            if (settings.softwareFormat is { } softwareFormat)
                native.setSoftwareFormat(softwareFormat.sampleRate, softwareFormat.speakerMode, softwareFormat.rawSpeakerCount).ThrowIfNotOk();

            if (settings.dspBuffer is { } dspBuffer)
                native.setDSPBufferSize(dspBuffer.length, dspBuffer.count).ThrowIfNotOk();

            int maxChannels = settings.maxChannels ?? 4095;
            native.init(maxChannels, settings.initFlags ?? INITFLAGS.NORMAL, IntPtr.Zero).ThrowIfNotOk();
            nativeInitialized = true;
            Volatile.Write(ref _maxChannels, maxChannels);

            native.getSoftwareChannels(out int softwareChannelCount).ThrowIfNotOk();
            Volatile.Write(ref _softwareChannelCount, softwareChannelCount);

            native.getSoftwareFormat(out int sampleRate, out _, out _).ThrowIfNotOk();
            Volatile.Write(ref _outputSampleRate, sampleRate);
        }

        List<ISoundSystemResource> BeginResourceRelease(LifecycleState nextState)
        {
            nativeLock.EnterWriteLock();

            try
            {
                ThrowIfLifecycleChangeUnavailable();

                lifecycleState = nextState;
                Interlocked.Exchange(ref queuedDisposals, null);

                if (nextState == LifecycleState.Disposed)
                    systemLists.TryRemove(native.handle, out _);

                List<ISoundSystemResource> resources = new(ownedResources.Keys);
                ownedResources.Clear();
                return resources;
            }
            finally
            {
                nativeLock.ExitWriteLock();
            }
        }

        void ThrowIfLifecycleChangeUnavailable()
        {
            if (lifecycleState == LifecycleState.Disposed)
                throw new ObjectDisposedException(nameof(SoundSystem), "The FMOD sound system has already been disposed.");

            if (lifecycleState == LifecycleState.Resetting)
                throw new InvalidOperationException("The FMOD sound system lifecycle cannot be changed while it is resetting.");
        }

        void ReleaseOwnedResources(List<ISoundSystemResource> resources)
        {
            lock (resourceDisposalLock)
            {
                while (activeResourceDisposals != 0)
                    Monitor.Wait(resourceDisposalLock);
            }

            // 진행 중이던 즉시 해제가 실패해 되돌린 소유권까지 현재 lifecycle 작업으로 넘깁니다.
            resources.AddRange(ownedResources.Keys);
            ownedResources.Clear();
            resources.Sort((left, right) => GetReleaseOrder(left).CompareTo(GetReleaseOrder(right)));

            int previousOrder = 0;
            bool hasPreviousOrder = false;

            foreach (ISoundSystemResource resource in resources)
            {
                int currentOrder = GetReleaseOrder(resource);

                if (nativeInitialized && hasPreviousOrder && previousOrder < currentOrder)
                    native.update().LogErrorIfNotOk();

                try
                {
                    resource.ReleaseUnmanagedResources();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                previousOrder = currentOrder;
                hasPreviousOrder = true;
            }

            if (nativeInitialized)
                native.update().LogErrorIfNotOk();
        }

        static int GetReleaseOrder(ISoundSystemResource resource) => resource switch
        {
            SoundChannel => -1,
            SoundChannelGroup => -1,
            Processing.DSP => 1,
            WaveAudioClip => 2,
            _ => 0
        };
    }
}
