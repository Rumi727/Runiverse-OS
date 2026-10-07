#nullable enable
namespace RuniOS.Sounds
{
    public sealed partial class WaveAudioClip
    {
        public bool Execute(Action<WaveAudioClip> action)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (_isDisposed)
                    return false;

                action.Invoke(this);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public bool Execute<T>(Func<WaveAudioClip, T> func, out T? result)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (_isDisposed)
                {
                    result = default;
                    return false;
                }

                result = func.Invoke(this);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }
    }
}