#nullable enable
using System.Threading;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Threading
{
    public static partial class UnityThread
    {
        public static int mainThreadId { get; private set; }
        public static bool isMainThread => Thread.CurrentThread.ManagedThreadId == mainThreadId;

        [OnCodeLoaded]
        static void OnCodeLoaded() => mainThreadId = Thread.CurrentThread.ManagedThreadId;

        public static void ThrowIfNotMainThread()
        {
            if (!isMainThread)
                throw new InvalidOperationException("Work can only be done on the main thread");
        }
    }
}