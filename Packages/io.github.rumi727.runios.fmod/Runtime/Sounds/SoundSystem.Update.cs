#nullable enable
using System.Threading;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public event Action? onUpdate;

        public void Update()
        {
            nativeLock.EnterReadLock();

            try
            {
                ThrowIfUnavailableUnsafe();
                native.update().LogErrorIfNotOk();
            }
            finally
            {
                nativeLock.ExitReadLock();
            }

            DisposeQueuedResources();

            onUpdate.SafeInvoke();
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
