#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace RuniOS.PackageManagement.Unity.Editor.Internal
{
    internal static class UnityUpmClient
    {
        internal static readonly SemaphoreSlim gate = new(1, 1);
        internal static async Task<T> WaitAsync<T>(Request<T> request, CancellationToken cancellationToken)
        {
            // A cancelled native request still drains before the caller releases the UPM gate.
            while (!await EditorThread.RunAsync(() => request.IsCompleted, CancellationToken.None).ConfigureAwait(false))
                await Task.Delay(50).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await EditorThread.RunAsync(() => request.Status == StatusCode.Success ? request.Result :
                throw new InvalidOperationException(request.Error?.message ?? "The Unity Package Manager request failed."), CancellationToken.None).ConfigureAwait(false);
        }
        internal static async Task<PackageInfo[]> ListAsync(CancellationToken cancellationToken)
        {
            ListRequest request = await EditorThread.RunAsync(() => Client.List(true, true), cancellationToken).ConfigureAwait(false);
            return (await WaitAsync(request, cancellationToken).ConfigureAwait(false)).ToArray();
        }
    }
}
