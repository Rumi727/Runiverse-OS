#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Booting;
using RuniOS.LowLevel;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace RuniOS.Threading
{
    public static partial class ThreadDispatcher
    {
        public const int allottedTime = 3;

        static readonly Stopwatch stopwatch = new Stopwatch();
        static readonly ConcurrentQueue<Action> scheduledTasks = new ConcurrentQueue<Action>();

        [Awaken]
        static void Awaken()
        {
            RuniPlayerLoop.onUpdate += Update;
            Kernel.quitting += BeginShutdown;
        }

#if UNITY_EDITOR
        [Unity.Scripting.LifecycleManagement.OnCodeLoaded]
        static void OnCodeLoaded()
        {
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

        static void BeginShutdown() => ForceScheduledTasksExecute();

        static void Update()
        {
            stopwatch.Restart();

            /*
             * 순서 중요!
             * 시간 초과 코드가 맨 뒤에 있을 경우 작업 리스트에서는 빠지는데 시간 초과로 인해 코드가 작동하지 않는 경우가 생김!!!
             */

            while (stopwatch.Elapsed.TotalMilliseconds < allottedTime && scheduledTasks.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        static void ForceScheduledTasksExecute()
        {
            while (scheduledTasks.TryDequeue(out Action action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static void ExecuteForget(Action action)
        {
            if (UnityThread.isMainThread)
                action.Invoke();
            else
                scheduledTasks.Enqueue(action);
        }

        public static UniTask Execute(Action action)
        {
            UniTaskCompletionSource tcs = new UniTaskCompletionSource();
            if (UnityThread.isMainThread)
            {
                InternalAction();
                return tcs.Task;
            }

            void InternalAction()
            {
                try
                {
                    action.Invoke();
                    tcs.TrySetResult();
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }

            scheduledTasks.Enqueue(InternalAction);
            return tcs.Task;
        }

        public static UniTask<T> Execute<T>(Func<T> func)
        {
            UniTaskCompletionSource<T> tcs = new UniTaskCompletionSource<T>();
            if (UnityThread.isMainThread)
            {
                InternalAction();
                return tcs.Task;
            }

            void InternalAction()
            {
                try
                {
                    T returnValue = func.Invoke();
                    tcs.TrySetResult(returnValue);
                }
                catch (Exception e)
                {
                    tcs.TrySetException(e);
                }
            }

            scheduledTasks.Enqueue(InternalAction);
            return tcs.Task;
        }
    }
}
