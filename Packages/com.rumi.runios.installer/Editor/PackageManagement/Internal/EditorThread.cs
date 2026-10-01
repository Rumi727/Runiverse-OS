#nullable enable
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEditor;

namespace RuniOS.PackageManagement.Unity.Editor.Internal
{
    [InitializeOnLoad]
    internal static class EditorThread
    {
        static readonly ConcurrentQueue<Action> callbacks = new();
        static EditorThread() => EditorApplication.update += Drain;
        static void Drain() { while (callbacks.TryDequeue(out Action? action)) action?.Invoke(); }
        internal static Task<T> RunAsync<T>(Func<T> action, CancellationToken cancellationToken)
        {
            TaskCompletionSource<T> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            int state = 0;
            CancellationTokenRegistration registration = cancellationToken.Register(() =>
            {
                if (Interlocked.CompareExchange(ref state, 2, 0) == 0) completion.TrySetCanceled(cancellationToken);
            });
            callbacks.Enqueue(() =>
            {
                if (Interlocked.CompareExchange(ref state, 1, 0) != 0) return;
                if (cancellationToken.IsCancellationRequested) { completion.TrySetCanceled(cancellationToken); return; }
                try { completion.TrySetResult(action()); } catch (Exception exception) { completion.TrySetException(exception); }
            });
            return CompleteAsync(completion.Task, registration);
        }
        static async Task<T> CompleteAsync<T>(Task<T> completion, CancellationTokenRegistration registration)
        {
            try { return await completion.ConfigureAwait(false); }
            finally { registration.Dispose(); }
        }
    }
}
