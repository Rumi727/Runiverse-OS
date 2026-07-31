#nullable enable
using FMOD;
using System.Collections.Concurrent;
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem : IDisposable
    {
        static readonly ConcurrentDictionary<IntPtr, SoundSystem> systemLists = [];

        public static SoundSystem main { get; } = new SoundSystem();

        SoundSystem()
        {
#if UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
            const uint stackSize = 1024 * 1024;

            FMOD.Thread.SetAttributes(THREAD_TYPE.NONBLOCKING, THREAD_AFFINITY.GROUP_DEFAULT, THREAD_PRIORITY.DEFAULT, (THREAD_STACK_SIZE)stackSize).ThrowIfNotOk();
            FMOD.Thread.SetAttributes(THREAD_TYPE.FILE, THREAD_AFFINITY.GROUP_DEFAULT, THREAD_PRIORITY.DEFAULT, (THREAD_STACK_SIZE)stackSize).ThrowIfNotOk();
            FMOD.Thread.SetAttributes(THREAD_TYPE.STREAM, THREAD_AFFINITY.GROUP_DEFAULT, THREAD_PRIORITY.DEFAULT, (THREAD_STACK_SIZE)stackSize).ThrowIfNotOk();
#endif

            listeners = new Listeners(this);

            Factory.System_Create(out native).ThrowIfNotOk();
            native.init(4095, INITFLAGS.NORMAL, IntPtr.Zero).ThrowIfNotOk();

            systemLists[native.handle] = this;
        }

        FMOD.System native;
        readonly ReaderWriterLockSlim nativeLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        readonly ConcurrentDictionary<ISoundSystemResource, byte> ownedResources = [];
        ConcurrentQueue<ISoundSystemResource>? queuedDisposals = [];

        // nativeLock 밖에서 실행 중인 리소스 해제 호출 수입니다.
        readonly object resourceDisposalLock = new();
        int activeResourceDisposals = 0;

        public bool isDisposed => Volatile.Read(ref _isDisposed);
        bool _isDisposed = false;

        public static SoundSystem GetManaged(IntPtr handle) => systemLists.GetValueOrDefault(handle);

        public void Register(ISoundSystemResource resource)
        {
            if (resource.system != this)
                throw new InvalidOperationException();

            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
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
                if (_isDisposed || !ownedResources.TryRemove(resource, out _))
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
        /// Requests made after this system begins disposal are ignored.<br/>
        /// 반복 요청할 수 있습니다. 등록된 리소스를 먼저 확보한 해제만 리소스를 해제하며 이후 큐 또는 즉시 요청은 무시됩니다.<br/>
        /// 이 시스템의 해제가 시작된 뒤 요청은 무시됩니다.
        /// </remarks>
        public void QueueDispose(ISoundSystemResource resource)
        {
            if (resource.system != this)
                throw new InvalidOperationException();

            if (isDisposed)
                return;

            ConcurrentQueue<ISoundSystemResource>? disposalQueue = Volatile.Read(ref queuedDisposals);
            disposalQueue?.Enqueue(resource);
        }

        /// <summary>
        /// Releases this sound system and all resources it still owns.<br/>
        /// 이 사운드 시스템과 여전히 소유하는 모든 리소스를 해제합니다.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// Thrown when called while the current thread holds the system native lock.<br/>
        /// 현재 스레드가 시스템 네이티브 잠금을 보유한 상태에서 호출한 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Repeated calls are ignored. This method prevents new resource disposal, waits for in-progress resource disposal to finish, then releases the native system.<br/>
        /// 반복 호출은 무시됩니다. 이 메서드는 새 리소스 해제를 차단하고 진행 중인 리소스 해제가 끝나기를 기다린 뒤 네이티브 시스템을 해제합니다.
        /// </remarks>
        public void Dispose()
        {
            ThrowIfSystemLockHeld();

            List<ISoundSystemResource> resources;

            nativeLock.EnterWriteLock();

            try
            {
                if (_isDisposed)
                    return;

                Volatile.Write(ref _isDisposed, true);
                Interlocked.Exchange(ref queuedDisposals, null);

                systemLists.TryRemove(native.handle, out _);

                // 소유권을 종료 경로로 넘기는 동안 write lock으로 새 등록과 리소스 해제 확보를 막습니다.
                resources = new List<ISoundSystemResource>(ownedResources.Keys);
                ownedResources.Clear();
            }
            finally
            {
                nativeLock.ExitWriteLock();
            }

            lock (resourceDisposalLock)
            {
                // 실행 중인 모든 해제는 nativeLock을 풀기 전에 이 수를 증가시킵니다.
                while (activeResourceDisposals != 0)
                    Monitor.Wait(resourceDisposalLock);
            }

            // 종료 중 실패한 즉시 해제는 activeResourceDisposals를 줄이기 전에 소유권을 되돌립니다.
            // 대기 뒤 한 번 더 수집하면 해당 리소스도 종료 순서에 포함됩니다.
            resources.AddRange(ownedResources.Keys);
            ownedResources.Clear();

            resources.Sort((left, right) => GetReleaseOrder(left).CompareTo(GetReleaseOrder(right)));

            int previousOrder = 0;
            bool hasPreviousOrder = false;

            foreach (ISoundSystemResource resource in resources)
            {
                int currentOrder = GetReleaseOrder(resource);

                if (hasPreviousOrder && previousOrder < currentOrder)
                    native.update().LogErrorIfNotOk();

                try
                {
                    resource.ReleaseUnmanagedResources();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Debug.RuntimeLogWarning($"Failed to release {resource.GetType().Name} before FMOD system shutdown. Native memory may leak.", nameof(SoundSystem));
                }

                previousOrder = currentOrder;
                hasPreviousOrder = true;
            }

            // DSP release 뒤 FMOD의 지연된 DSP 제거를 반영합니다.
            native.update().LogErrorIfNotOk();

            nativeLock.EnterWriteLock();

            try
            {
                native.release().ThrowIfNotOk();
                native.clearHandle();
            }
            finally
            {
                nativeLock.ExitWriteLock();
            }
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
