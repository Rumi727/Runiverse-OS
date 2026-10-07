#nullable enable
namespace RuniOS.Sounds.Processing
{
    public abstract partial class DSP
    {
        /// <summary>
        /// Invokes <paramref name="action"/> while this DSP cannot be disposed.<br/>
        /// 이 DSP가 해제될 수 없는 동안 <paramref name="action"/>을 호출합니다.
        /// </summary>
        /// <param name="action">
        /// Action that uses native FMOD DSP.<br/>
        /// 네이티브 FMOD DSP를 사용하는 작업입니다.
        /// </param>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this DSP has been disposed or is being disposed.<br/>
        /// 이 DSP가 해제되었거나 해제 중인 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Do not retain or use supplied native DSP after <paramref name="action"/> returns.<br/>
        /// <paramref name="action"/>이 반환된 뒤에는 전달받은 네이티브 DSP를 보관하거나 사용하면 안 됩니다.
        /// </remarks>
        public void UseNative(Action<FMOD.DSP> action)
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

        public void UseNative<T>(Action<FMOD.DSP, T> action, T state)
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
        /// Invokes <paramref name="func"/> while this DSP cannot be disposed and returns its result.<br/>
        /// 이 DSP가 해제될 수 없는 동안 <paramref name="func"/>을 호출하고 결과를 반환합니다.
        /// </summary>
        /// <typeparam name="TResult">
        /// Type of value returned by <paramref name="func"/>.<br/>
        /// <paramref name="func"/>이 반환하는 값의 형식입니다.
        /// </typeparam>
        /// <param name="func">
        /// Function that uses native FMOD DSP.<br/>
        /// 네이티브 FMOD DSP를 사용하는 함수입니다.
        /// </param>
        /// <returns>
        /// Value returned by <paramref name="func"/>.<br/>
        /// <paramref name="func"/>이 반환한 값을 반환합니다.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this DSP has been disposed or is being disposed.<br/>
        /// 이 DSP가 해제되었거나 해제 중인 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// Do not retain or use supplied native DSP after <paramref name="func"/> returns.<br/>
        /// <paramref name="func"/>이 반환된 뒤에는 전달받은 네이티브 DSP를 보관하거나 사용하면 안 됩니다.
        /// </remarks>
        public TResult UseNative<TResult>(Func<FMOD.DSP, TResult> func)
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

        public TResult UseNative<TResult, T>(Func<FMOD.DSP, T, TResult> func, T state)
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

        internal static void UseNativePair(DSP first, DSP second, Action<FMOD.DSP, FMOD.DSP> action)
        {
            if (ReferenceEquals(first, second))
            {
                first.UseNative(native => action.Invoke(native, native));
                return;
            }

            DSP lower = first.native.handle.ToInt64() < second.native.handle.ToInt64() ? first : second;
            DSP higher = ReferenceEquals(lower, first) ? second : first;

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

        internal static T UseNativePair<T>(DSP first, DSP second, Func<FMOD.DSP, FMOD.DSP, T> func)
        {
            if (ReferenceEquals(first, second))
                return first.UseNative(native => func.Invoke(native, native));

            DSP lower = first.nativeHandle.ToInt64() < second.nativeHandle.ToInt64() ? first : second;
            DSP higher = ReferenceEquals(lower, first) ? second : first;

            lower.nativeLock.EnterReadLock();
            higher.nativeLock.EnterReadLock();

            try
            {
                first.ThrowIfDisposedUnsafe();
                second.ThrowIfDisposedUnsafe();

                return func.Invoke(first.native, second.native);
            }
            finally
            {
                higher.nativeLock.ExitReadLock();
                lower.nativeLock.ExitReadLock();
            }
        }
    }
}
