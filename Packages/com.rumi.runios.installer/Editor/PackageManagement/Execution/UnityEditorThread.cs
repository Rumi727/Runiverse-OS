#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Runs editor API access on the main thread.<br/>
    /// editor API 접근을 main thread에서 실행합니다.
    /// </summary>
    public static class UnityEditorThread
    {
        /// <summary>
        /// Switches to the main thread and invokes the action.<br/>
        /// main thread로 전환하고 작업을 호출합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The action result type.<br/>
        /// 작업 결과 타입입니다.
        /// </typeparam>
        /// <param name="action">
        /// The editor action.<br/>
        /// editor 작업입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token checked before invocation.<br/>
        /// 호출 전에 확인하는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// The result produced by the action.<br/>
        /// 작업이 생산한 결과를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when action is null.<br/>
        /// action이 null이면 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when cancellation is requested before invocation.<br/>
        /// 호출 전에 취소가 요청되면 발생합니다.
        /// </exception>
        public static async Task<T> RunAsync<T>(Func<T> action, CancellationToken cancellationToken = default)
        {
            if (action is null) throw new ArgumentNullException(nameof(action));
            cancellationToken.ThrowIfCancellationRequested();
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();
            return action();
        }
    }
}
