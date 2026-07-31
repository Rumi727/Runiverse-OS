#nullable enable
using RuniOS.Booting;
using RuniOS.LowLevel;
using RuniOS.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace RuniOS.Resource
{
    public static partial class DisposeQueue
    {
        public const int allottedTime = 10;

        static readonly Stopwatch stopwatch = new Stopwatch();
        static readonly ConcurrentQueue<Action> scheduledTasks = [];

        static volatile bool shutdownStarted = false;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetShutdownState() => shutdownStarted = false;

        [Awaken]
        static void Awaken()
        {
            RuniPlayerLoop.onPostLateUpdate += Update;
            Kernel.quitting += BeginShutdown;
        }

#if UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnCodeLoaded]
        static void OnCodeLoaded()
        {
            ResetShutdownState();

            UnityEditor.EditorApplication.update += EditorUpdate;
            UnityEditor.EditorApplication.quitting += BeginShutdown;
        }

        [Unity.Scripting.LifecycleManagement.OnCodeUnloading]
        static void OnCodeUnloading()
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.EditorApplication.quitting -= BeginShutdown;

            BeginShutdown();
        }

        static void EditorUpdate()
        {
            if (!Kernel.isPlaying || UnityEditor.EditorApplication.isPaused)
                Update();
        }
#endif

        static void BeginShutdown()
        {
            shutdownStarted = true;
            ForceScheduledTasksExecute();
        }

        static void Update()
        {
            stopwatch.Restart();

            /*
             * 순서 중요!
             * 시간 초과 코드가 맨 뒤에 있을 경우 작업 리스트에서는 빠지는데 시간 초과로 인해 코드가 작동하지 않는 경우가 생김!!!
             */

            while (stopwatch.Elapsed.TotalMilliseconds < allottedTime && scheduledTasks.TryDequeue(out var disposable))
            {
                try
                {
                    disposable.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        static void ForceScheduledTasksExecute()
        {
            while (scheduledTasks.TryDequeue(out var disposable))
            {
                try
                {
                    disposable.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static void Enqueue(IDisposable? disposable)
        {
            if (disposable.IsNull())
                return;

            Enqueue(disposable.Dispose);
        }

        public static void Enqueue(Action action)
        {
            if (!UnityThread.isMainThread)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                return;
            }

            int executed = 0;
            void DisposeOnce()
            {
                // ReSharper disable once AccessToModifiedClosure
                if (Interlocked.Exchange(ref executed, 1) != 0)
                    return;

                action.Invoke();
            }

            scheduledTasks.Enqueue(DisposeOnce);

            if (shutdownStarted)
            {
                try
                {
                    DisposeOnce();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }
}
