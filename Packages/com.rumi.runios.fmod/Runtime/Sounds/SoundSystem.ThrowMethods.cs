#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        void ThrowIfDisposedUnsafe()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SoundSystem), "The FMOD sound system has already been disposed.");
        }

        void ThrowIfSystemLockHeld()
        {
            if (nativeLock.IsReadLockHeld || nativeLock.IsUpgradeableReadLockHeld || nativeLock.IsWriteLockHeld)
                throw new InvalidOperationException();
        }
    }
}
