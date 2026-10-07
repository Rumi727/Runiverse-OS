#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundChannelGroup
    {
        /// <summary>
        /// Adds <paramref name="group"/> as an input to this channel group.<br/>
        /// <paramref name="group"/>을 이 채널 그룹의 입력으로 추가합니다.
        /// </summary>
        /// <param name="group">
        /// The channel group to add.<br/>
        /// 추가할 채널 그룹입니다.
        /// </param>
        /// <param name="propagateDSPClock">
        /// <see langword="true"/> to recursively propagate this group's DSP clock values; otherwise, <see langword="false"/>.<br/>
        /// 이 그룹의 DSP 클록 값을 재귀적으로 전파하려면 <see langword="true"/>이고, 그렇지 않으면 <see langword="false"/>입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="group"/> is <see langword="null"/>.<br/>
        /// <paramref name="group"/>이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this channel group or <paramref name="group"/> has been disposed.<br/>
        /// 이 채널 그룹 또는 <paramref name="group"/>이 해제된 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// This method directly wraps FMOD <c>ChannelGroup.addGroup</c>. It does not track, validate, or own the resulting hierarchy.<br/>
        /// 이 메서드는 FMOD <c>ChannelGroup.addGroup</c>을 직접 래핑합니다. 생성된 하이어라키를 추적, 검증 또는 소유하지 않습니다.
        /// </remarks>
        public void AddGroup(SoundChannelGroup group, bool propagateDSPClock = true)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            UseNativePair(this, group, (parent, child) => parent.addGroup(child, propagateDSPClock).ThrowIfNotOk());
        }
    }
}