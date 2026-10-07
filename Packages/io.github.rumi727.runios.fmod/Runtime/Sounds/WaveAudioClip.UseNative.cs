#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        public void UseNative(Action<Sound> action)
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

        public T UseNative<T>(Func<Sound, T> func)
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
    }
}