#nullable enable
using FMOD;
using System.Threading;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Safely owns an FMOD channel group created by <see cref="SoundSystem"/>.<br/>
    /// <see cref="SoundSystem"/>에서 생성한 FMOD 채널 그룹을 안전하게 소유합니다.
    /// </summary>
    /// <remarks>
    /// This wrapper protects only the native handle lifetime. FMOD remains the source of truth for group hierarchy and routing.<br/>
    /// 이 래퍼는 네이티브 핸들 수명만 보호합니다. 그룹 하이어라키와 라우팅의 기준은 FMOD입니다.
    /// </remarks>
    public sealed partial class SoundChannelGroup : IDisposable, ISoundSystemResource
    {
        public static class Unsafe
        {
            public static SoundChannelGroup CreateInstance(SoundSystem system, ChannelGroup channelGroup) => new SoundChannelGroup(system, channelGroup);
        }

        SoundChannelGroup(SoundSystem system, ChannelGroup channelGroup)
        {
            this.system = system;
            native = channelGroup;
            nativeHandle = native.handle;

            system.Register(this);
        }

        /// <summary>
        /// Gets the sound system that created this channel group.<br/>
        /// 이 채널 그룹을 생성한 사운드 시스템을 가져옵니다.
        /// </summary>
        public SoundSystem system { get; }

        ChannelGroup native;
        readonly IntPtr nativeHandle;
        readonly ReaderWriterLockSlim nativeLock = new(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Gets whether this channel group has been disposed.<br/>
        /// 이 채널 그룹이 해제되었는지 여부를 가져옵니다.
        /// </summary>
        public bool isDisposed => Volatile.Read(ref _isDisposed);
        bool _isDisposed;

        /// <summary>
        /// Releases this channel group and unregisters it from its <see cref="SoundChannelGroup.system"/>.<br/>
        /// 이 채널 그룹을 해제하고 <see cref="SoundChannelGroup.system"/>의 소유 리소스에서 제거합니다.
        /// </summary>
        /// <remarks>
        /// Repeated calls are ignored after this channel group has been disposed.<br/>
        /// 이 채널 그룹이 해제된 뒤의 반복 호출은 무시됩니다.
        /// </remarks>
        public void Dispose() => system.Dispose(this);

        ~SoundChannelGroup() => SoundSystem.LogUndisposedResource(this);

        void ISoundSystemResource.ReleaseUnmanagedResources()
        {
            nativeLock.EnterWriteLock();

            try
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;

                RESULT result = native.release();
                if (result != RESULT.OK && result != RESULT.ERR_INVALID_HANDLE)
                    result.ThrowIfNotOk();

            }
            finally
            {
                native.clearHandle();
                nativeLock.ExitWriteLock();
            }
        }
    }
}
