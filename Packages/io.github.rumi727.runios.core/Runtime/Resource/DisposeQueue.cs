#nullable enable
using RuniOS.LowLevel;
using RuniOS.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace RuniOS.Resource
{
    public static partial class DisposeQueue
    {
        public const int allottedTime = 10;

        static readonly Stopwatch stopwatch = new Stopwatch();
        static readonly ConcurrentQueue<Action> scheduledTasks = [];

        [AutoStaticsCleanup]
        static volatile bool shutdownStarted = false;

        [OnEnteringPlayMode]
        static void OnEnteringPlayMode() => RuniPlayerLoop.onPostLateUpdate += Update;

        [OnExitingPlayMode]
        static void OnExitingPlayMode()
        {
            RuniPlayerLoop.onPostLateUpdate -= Update;
            BeginShutdown();
        }

#if UNITY_EDITOR
        [UnityEditor.Scripting.LifecycleManagement.OnEnteringEditMode]
        static void OnCodeLoaded()
        {
            UnityEditor.EditorApplication.update += Update;
            UnityEditor.EditorApplication.quitting += BeginShutdown;
        }

        [UnityEditor.Scripting.LifecycleManagement.OnExitingEditMode]
        static void OnCodeUnloading()
        {
            UnityEditor.EditorApplication.update -= Update;
            UnityEditor.EditorApplication.quitting -= BeginShutdown;

            BeginShutdown();
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
