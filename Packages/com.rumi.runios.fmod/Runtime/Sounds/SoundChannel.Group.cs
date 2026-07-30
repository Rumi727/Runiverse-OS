#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundChannel
    {
        /// <summary>
        /// Sets the channel group that receives this channel's output.<br/>
        /// 이 채널의 출력을 받을 채널 그룹을 설정합니다.
        /// </summary>
        /// <param name="group">
        /// The channel group to receive this channel's output.<br/>
        /// 이 채널의 출력을 받을 채널 그룹입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="group"/> is <see langword="null"/>.<br/>
        /// <paramref name="group"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this channel or <paramref name="group"/> has been disposed.<br/>
        /// 이 채널 또는 <paramref name="group"/>이 해제된 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// This method directly wraps FMOD <c>Channel.setChannelGroup</c>. FMOD determines routing validity.<br/>
        /// 이 메서드는 FMOD <c>Channel.setChannelGroup</c>을 직접 래핑합니다. 라우팅 유효성은 FMOD가 결정합니다.
        /// </remarks>
        public void SetChannelGroup(SoundChannelGroup group)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            group.UseNative((channelGroup, native) => native.setChannelGroup(channelGroup).ThrowIfNotOk(), native);
        }
    }
}