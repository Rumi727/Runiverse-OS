#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        void ThrowIfDisposedUnsafe()
        {
            Debug.Assert(nativeLock.IsReadLockHeld || nativeLock.IsWriteLockHeld, "The WaveAudioClip native lock must be held.");

            if (_isDisposed)
                throw new ObjectDisposedException(nameof(WaveAudioClip), "The FMOD wave audio clip has already been disposed.");
        }
    }
}
