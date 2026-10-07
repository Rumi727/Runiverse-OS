#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public bool Execute(Action<SoundSystem> action)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (lifecycleState != LifecycleState.Active)
                    return false;

                action.Invoke(this);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public bool Execute<T>(Action<SoundSystem, T> action, T state)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (lifecycleState != LifecycleState.Active)
                    return false;

                action.Invoke(this, state);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public bool Execute<T1, T2>(Action<SoundSystem, T1, T2> action, T1 arg1, T2 arg2)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (lifecycleState != LifecycleState.Active)
                    return false;

                action.Invoke(this, arg1, arg2);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public bool Execute<TResult>(Func<SoundSystem, TResult> func, out TResult? result)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (lifecycleState != LifecycleState.Active)
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

        public bool Execute<T, TResult>(Func<SoundSystem, T, TResult> func, T state, out TResult? result)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (lifecycleState != LifecycleState.Active)
                {
                    result = default;
                    return false;
                }

                result = func.Invoke(this, state);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public bool Execute<T1, T2, TResult>(Func<SoundSystem, T1, T2, TResult> func, T1 arg1, T2 arg2, out TResult? result)
        {
            nativeLock.EnterReadLock();

            try
            {
                if (lifecycleState != LifecycleState.Active)
                {
                    result = default;
                    return false;
                }

                result = func.Invoke(this, arg1, arg2);
                return true;
            }
            finally
            {
                nativeLock.ExitReadLock();
            }
        }

        public UniTask<bool> ExecuteOnThreadPool(Action<SoundSystem> action) => UniTask.RunOnThreadPool(() => Execute(action));

        public UniTask<(bool success, T? result)> ExecuteOnThreadPool<T>(Func<SoundSystem, T> func) => UniTask.RunOnThreadPool(() =>
        {
            bool success = Execute(func, out T? result);
            return (success, result);
        });
    }
}
