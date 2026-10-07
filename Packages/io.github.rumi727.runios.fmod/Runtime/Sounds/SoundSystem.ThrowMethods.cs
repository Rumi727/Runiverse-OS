#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        void ThrowIfUnavailableUnsafe()
        {
            switch (lifecycleState)
            {
                case LifecycleState.Active:
                    return;

                case LifecycleState.Disposed:
                    throw new ObjectDisposedException(nameof(SoundSystem), "The FMOD sound system has already been disposed.");

                default:
                    throw new InvalidOperationException("The FMOD sound system is resetting or failed to initialize.");
            }
        }

        void ThrowIfSystemLockHeld()
        {
            if (nativeLock.IsReadLockHeld || nativeLock.IsUpgradeableReadLockHeld || nativeLock.IsWriteLockHeld)
                throw new InvalidOperationException();
        }
    }
}
