#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        /// <summary>
        /// Creates and registers an FMOD channel group.<br/>
        /// FMOD 채널 그룹을 생성하고 등록합니다.
        /// </summary>
        /// <param name="name">
        /// The name used to identify the channel group in FMOD.<br/>
        /// FMOD에서 채널 그룹을 식별하는 데 사용할 이름입니다.
        /// </param>
        /// <returns>
        /// The created channel group.<br/>
        /// 생성한 채널 그룹을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system has been disposed.<br/>
        /// 이 사운드 시스템이 해제된 경우 발생합니다.
        /// </exception>
        public SoundChannelGroup CreateChannelGroup(string name)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                native.createChannelGroup(name, out ChannelGroup channelGroup).ThrowIfNotOk();

                try
                {
                    return new SoundChannelGroup(this, channelGroup);
                }
                catch
                {
                    channelGroup.release().LogErrorIfNotOk();
                    channelGroup.clearHandle();
                    throw;
                }
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }
    }
}
