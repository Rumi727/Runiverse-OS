#nullable enable
using System.Threading;
using UnityEngine;

namespace RuniOS.Threading
{
    public static class UnityThread
    {
        public static int mainThreadId { get; private set; }
        public static bool isMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void RuntimeInit() => mainThreadId = Thread.CurrentThread.ManagedThreadId;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void EditorInit() => mainThreadId = Thread.CurrentThread.ManagedThreadId;
#endif

        public static void ThrowIfNotMainThread()
        {
            if (!isMainThread)
                throw new InvalidOperationException("Work can only be done on the main thread");
        }
    }
}