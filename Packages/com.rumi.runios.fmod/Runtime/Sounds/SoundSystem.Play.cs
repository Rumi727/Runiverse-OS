#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        /// <summary>
        /// Starts playback of <paramref name="clip"/> and returns its owned channel.<br/>
        /// <paramref name="clip"/> 재생을 시작하고 소유 채널을 반환합니다.
        /// </summary>
        /// <param name="clip">
        /// The clip created by this sound system to play.<br/>
        /// 이 사운드 시스템이 생성한 재생할 클립입니다.
        /// </param>
        /// <param name="paused">
        /// <see langword="true"/> to create the channel paused; otherwise, <see langword="false"/>.<br/>
        /// 일시 정지된 채널로 생성하려면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// A channel that owns the playback until it is stopped or disposed.<br/>
        /// 중지하거나 해제할 때까지 재생을 소유하는 채널을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="clip"/> is <see langword="null"/>.<br/>
        /// <paramref name="clip"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="clip"/> was created by a different sound system.<br/>
        /// <paramref name="clip"/>이 다른 사운드 시스템에서 생성된 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system or <paramref name="clip"/> has been disposed.<br/>
        /// 이 사운드 시스템 또는 <paramref name="clip"/>이 해제된 경우 발생합니다.
        /// </exception>
        public SoundChannel PlaySound(WaveAudioClip clip, bool paused = false)
        {
            ValidatePlayableClip(clip);
            return PlaySoundUnsafe(clip, default, paused);
        }

        /// <summary>
        /// Starts playback of <paramref name="clip"/> in <paramref name="group"/> and returns its owned channel.<br/>
        /// <paramref name="clip"/>을 <paramref name="group"/>에서 재생하고 소유 채널을 반환합니다.
        /// </summary>
        /// <param name="clip">
        /// The clip created by this sound system to play.<br/>
        /// 이 사운드 시스템이 생성한 재생할 클립입니다.
        /// </param>
        /// <param name="group">
        /// The channel group to receive the playback output.<br/>
        /// 재생 출력을 받을 채널 그룹입니다.
        /// </param>
        /// <param name="paused">
        /// <see langword="true"/> to create the channel paused; otherwise, <see langword="false"/>.<br/>
        /// 일시 정지된 채널로 생성하려면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// A channel that owns the playback until it is stopped or disposed.<br/>
        /// 중지하거나 해제할 때까지 재생을 소유하는 채널을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="clip"/> or <paramref name="group"/> is <see langword="null"/>.<br/>
        /// <paramref name="clip"/> 또는 <paramref name="group"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="clip"/> was created by a different sound system.<br/>
        /// <paramref name="clip"/>이 다른 사운드 시스템에서 생성된 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this sound system, <paramref name="clip"/>, or <paramref name="group"/> has been disposed.<br/>
        /// 이 사운드 시스템, <paramref name="clip"/> 또는 <paramref name="group"/>이 해제된 경우 발생합니다.
        /// </exception>
        public SoundChannel PlaySound(WaveAudioClip clip, SoundChannelGroup group, bool paused = false)
        {
            ValidatePlayableClip(clip);

            if (group == null)
                throw new ArgumentNullException(nameof(group));

            return group.UseNative(channelGroup => PlaySoundUnsafe(clip, channelGroup, paused));
        }

        void ValidatePlayableClip(WaveAudioClip clip)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));

            if (clip.system != this)
                throw new ArgumentException("The FMOD wave audio clip belongs to a different sound system.", nameof(clip));
        }

        SoundChannel PlaySoundUnsafe(WaveAudioClip clip, ChannelGroup channelGroup, bool paused)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();
                return clip.UseNative(sound =>
                {
                    native.playSound(sound, channelGroup, true, out Channel channel).ThrowIfNotOk();

                    SoundChannel? soundChannel = null;

                    try
                    {
                        soundChannel = new SoundChannel(this, channel, clip);

                        if (!paused && !soundChannel.isDisposed)
                            channel.setPaused(false).ThrowIfNotOk(soundChannel);

                        return soundChannel;
                    }
                    catch
                    {
                        if (soundChannel != null)
                        {
                            try
                            {
                                soundChannel.Stop();
                            }
                            catch (Exception e)
                            {
                                Debug.LogException(e);
                            }
                        }
                        else
                        {
                            channel.stop().LogErrorIfNotOk();
                            channel.clearHandle();
                        }

                        throw;
                    }
                });
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }
    }
}
