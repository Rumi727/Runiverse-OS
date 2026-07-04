#nullable enable
using RuniOS.Booting;
using RuniOS.LowLevel;
using System.Collections.Concurrent;
using System.Diagnostics;
using UnityEngine.Scripting;

namespace RuniOS.Resource
{
    public static class DisposeQueue
    {
        public const int allottedTime = 10;

        static readonly Stopwatch stopwatch = new Stopwatch();
        static readonly ConcurrentQueue<IDisposable> disposables = [];

        [Awaken]
        [Preserve]
        public static void Awaken()
        {
            RuniPlayerLoop.onPostLateUpdate += Update;
            Kernel.quitting += Quitting;

#if UNITY_EDITOR
            RegisterEditorCallbacks();
#endif
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InitializeOnLoadMethod() => RegisterEditorCallbacks();

        static void RegisterEditorCallbacks()
        {
            UnityEditor.EditorApplication.update -= EditorUpdate;
            UnityEditor.EditorApplication.update += EditorUpdate;

            UnityEditor.EditorApplication.quitting -= ForceScheduledTasksExecute;
            UnityEditor.EditorApplication.quitting += ForceScheduledTasksExecute;

            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload -= ForceScheduledTasksExecute;
            UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += ForceScheduledTasksExecute;
        }

        static void EditorUpdate()
        {
            if (!Kernel.isPlaying || UnityEditor.EditorApplication.isPaused)
                Update();
        }
#endif

        static void Quitting() => ForceScheduledTasksExecute();

        static void Update()
        {
            stopwatch.Restart();

            /*
             * 순서 중요!
             * 시간 초과 코드가 맨 뒤에 있을 경우 작업 리스트에서는 빠지는데 시간 초과로 인해 코드가 작동하지 않는 경우가 생김!!!
             */

            while (stopwatch.Elapsed.TotalMilliseconds < allottedTime && disposables.TryDequeue(out var disposable))
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        static void ForceScheduledTasksExecute()
        {
            while (disposables.TryDequeue(out var disposable))
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        public static void Enqueue(IDisposable disposable) => disposables.Enqueue(disposable);
    }
}
