#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundChannelGroup
    {
        /// <summary>
        /// Invokes <paramref name="action"/> when this channel group has not been disposed.<br/>
        /// 이 채널 그룹이 해제되지 않은 경우 <paramref name="action"/>을 호출합니다.
        /// </summary>
        /// <param name="action">
        /// The action to invoke with this channel group.<br/>
        /// 이 채널 그룹과 함께 호출할 작업입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="action"/> was invoked; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="action"/>이 호출된 경우 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Execute(Action<SoundChannelGroup> action)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (_isDisposed)
                    return false;

                action.Invoke(this);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Invokes <paramref name="func"/> when this channel group has not been disposed.<br/>
        /// 이 채널 그룹이 해제되지 않은 경우 <paramref name="func"/>를 호출합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The type of value returned by <paramref name="func"/>.<br/>
        /// <paramref name="func"/>가 반환하는 값의 형식입니다.
        /// </typeparam>
        /// <param name="func">
        /// The function to invoke with this channel group.<br/>
        /// 이 채널 그룹과 함께 호출할 함수입니다.
        /// </param>
        /// <param name="result">
        /// Receives the value returned by <paramref name="func"/> when invoked; otherwise, the default value.<br/>
        /// <paramref name="func"/>가 호출된 경우 반환값을 받고, 그렇지 않으면 기본값을 받습니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="func"/> was invoked; otherwise, <see langword="false"/>.<br/>
        /// <paramref name="func"/>이 호출된 경우 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Execute<T>(Func<SoundChannelGroup, T> func, out T? result)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (_isDisposed)
                {
                    result = default;
                    return false;
                }

                result = func.Invoke(this);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }
    }
}