#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public void UseNative(Action<FMOD.System> action)
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

        public void UseNative<T>(Action<FMOD.System, T> action, T state)
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

        public TResult UseNative<TResult>(Func<FMOD.System, TResult> action)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                return action.Invoke(native);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public TResult UseNative<T, TResult>(Func<FMOD.System, T, TResult> action, T state)
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                return action.Invoke(native, state);
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }
    }
}