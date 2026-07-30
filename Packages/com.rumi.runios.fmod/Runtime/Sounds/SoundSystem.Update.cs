#nullable enable
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public void Update()
        {
            ThrowIfSystemLockHeld();
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfDisposedUnsafe();
                native.update().LogErrorIfNotOk();
            }
            finally
            {
                nativeLock.ExitReadLock();
            }

            DisposeQueuedResources();
        }

        void DisposeQueuedResources()
        {
            var disposalQueue = Volatile.Read(ref queuedDisposals);
            if (disposalQueue == null)
                return;

            while (disposalQueue.TryDequeue(out ISoundSystemResource? resource))
            {
                try
                {
                    Dispose(resource);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
