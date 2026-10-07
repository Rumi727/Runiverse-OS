#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    public sealed partial class SoundChannelGroup
    {
        /// <summary>
        /// Invokes <paramref name="action"/> while this channel group cannot be disposed.<br/>
        /// 이 채널 그룹이 해제될 수 없는 동안 <paramref name="action"/>을 호출합니다.
        /// </summary>
        /// <param name="action">
        /// The action that uses the native FMOD channel group.<br/>
        /// 네이티브 FMOD 채널 그룹을 사용하는 작업입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this channel group has been disposed.<br/>
        /// 이 채널 그룹이 해제된 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Do not retain or use the supplied native channel group after <paramref name="action"/> returns.<br/>
        /// <paramref name="action"/>이 반환된 뒤에는 전달받은 네이티브 채널 그룹을 보관하거나 사용하면 안 됩니다.
        /// </remarks>
        public void UseNative(Action<ChannelGroup> action)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                action.Invoke(native);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public void UseNative<T>(Action<ChannelGroup, T> action, T state)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                action.Invoke(native, state);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        /// <summary>
        /// Invokes <paramref name="func"/> while this channel group cannot be disposed and returns its result.<br/>
        /// 이 채널 그룹이 해제될 수 없는 동안 <paramref name="func"/>를 호출하고 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="TResult">
        /// The type of value returned by <paramref name="func"/>.<br/>
        /// <paramref name="func"/>가 반환하는 값의 형식입니다.
        /// </typeparam>
        /// <param name="func">
        /// The function that uses the native FMOD channel group.<br/>
        /// 네이티브 FMOD 채널 그룹을 사용하는 함수입니다.
        /// </param>
        /// <returns>
        /// The value returned by <paramref name="func"/>.<br/>
        /// <paramref name="func"/>이 반환한 값을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this channel group has been disposed.<br/>
        /// 이 채널 그룹이 해제된 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Do not retain or use the supplied native channel group after <paramref name="func"/> returns.<br/>
        /// <paramref name="func"/>이 반환된 뒤에는 전달받은 네이티브 채널 그룹을 보관하거나 사용하면 안 됩니다.
        /// </remarks>
        public TResult UseNative<TResult>(Func<ChannelGroup, TResult> func)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                return func.Invoke(native);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public TResult UseNative<T, TResult>(Func<ChannelGroup, T, TResult> func, T state)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                return func.Invoke(native, state);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        static void UseNativePair(SoundChannelGroup first, SoundChannelGroup second, Action<ChannelGroup, ChannelGroup> action)
        {
            if (ReferenceEquals(first, second))
            {
                first.UseNative(channelGroup => action.Invoke(channelGroup, channelGroup));
                return;
            }

            SoundChannelGroup lower = first.nativeHandle.ToInt64() < second.nativeHandle.ToInt64() ? first : second;
            SoundChannelGroup higher = ReferenceEquals(lower, first) ? second : first;

            // 두 그룹을 함께 사용하는 호출만 handle 순서로 잠가 반대 방향 호출의 교착을 막습니다.
            lower.nativeLock.EnterReadLock();
            higher.nativeLock.EnterReadLock();

            try
            {
                first.ThrowIfDisposedUnsafe();
                second.ThrowIfDisposedUnsafe();
                action.Invoke(first.native, second.native);
            }
            finally
            {
                higher.nativeLock.ExitReadLock();
                lower.nativeLock.ExitReadLock();
            }
        }
    }
}