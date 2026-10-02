#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Serializes native UPM requests shared by cooperating editor implementations.<br/>
    /// 협력하는 editor 구현들이 공유하는 native UPM 요청을 순차 실행합니다.
    /// </summary>
    public static class UnityUpmClient
    {
        static readonly SemaphoreSlim gate = new(1, 1);
        /// <summary>
        /// Starts a request on the main thread and drains native completion before releasing the gate.<br/>
        /// main thread에서 요청을 시작하고 native 완료를 기다린 뒤 gate를 해제합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The request result type.<br/>
        /// 요청 결과 타입입니다.
        /// </typeparam>
        /// <param name="requestFactory">
        /// The factory called once after acquiring the gate.<br/>
        /// gate 획득 후 한 번 호출하는 factory입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// Cancellation is reported after an already-started request finishes.<br/>
        /// 이미 시작된 요청은 완료 후 취소를 보고합니다.
        /// </param>
        /// <returns>
        /// The successful native request result.<br/>
        /// 성공한 native 요청 결과를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when requestFactory is null.<br/>
        /// requestFactory가 null이면 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when UPM reports failure.<br/>
        /// UPM이 실패를 보고하면 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when cancellation is requested, after draining an active request.<br/>
        /// 취소가 요청되면 활성 요청 완료 후 발생합니다.
        /// </exception>
        public static async Task<T> RunAsync<T>(Func<Request<T>> requestFactory, CancellationToken cancellationToken = default)
        {
            if (requestFactory is null) throw new ArgumentNullException(nameof(requestFactory));
            await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await Awaitable.MainThreadAsync();
                cancellationToken.ThrowIfCancellationRequested();
                Request<T> request = requestFactory();
                while (!request.IsCompleted)
                {
                    // UPM has no cancellation API. Never release the gate over an active request.
                    await Task.Delay(50).ConfigureAwait(false);
                    await Awaitable.MainThreadAsync();
                }
                cancellationToken.ThrowIfCancellationRequested();
                if (request.Status != StatusCode.Success)
                    throw new InvalidOperationException(request.Error?.message ?? "The Unity Package Manager request failed.");
                return request.Result;
            }
            finally { gate.Release(); }
        }
    }
}
