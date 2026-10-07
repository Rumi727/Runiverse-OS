#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class SoundChannelGroup
    {
        void ThrowIfDisposedUnsafe()
        {
            Debug.Assert(nativeLock.IsReadLockHeld || nativeLock.IsWriteLockHeld, "The SoundChannelGroup native lock must be held.");

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SoundChannelGroup), "The FMOD sound channel group has already been disposed.");
        }
    }
}
