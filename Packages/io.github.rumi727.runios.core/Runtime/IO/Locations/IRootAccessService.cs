#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Services;
using System.Threading;

namespace RuniOS.IO.Locations
{
    /// <summary>
    /// Requests access to a root that the application can legitimately use and provides it as an <see cref="IIOProvider"/>.<br/>
    /// 애플리케이션이 합법적으로 사용할 수 있는 루트에 대한 접근을 요청하고, 해당 루트를 <see cref="IIOProvider"/>로 제공합니다.
    /// </summary>
    public interface IRootAccessService : IService
    {
        /// <summary>
        /// Requests access to a new root and returns an <see cref="IIOProvider"/> whose root is the granted root.<br/>
        /// 새 루트에 대한 접근을 요청하고, 접근이 허용된 루트를 루트로 하는 <see cref="IIOProvider"/>를 반환합니다.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token used to cancel the operation.<br/>
        /// 작업을 취소하는 데 사용되는 취소 토큰입니다.
        /// </param>
        /// <returns>
        /// When the asynchronous operation completes, returns the provider for the granted root, or <see langword="null"/> if the user declined or canceled the request.<br/>
        /// 비동기 작업이 완료되면 접근이 허용된 루트의 프로바이더를 반환하며, 사용자가 요청을 거부하거나 취소한 경우 <see langword="null"/>을 반환합니다.
        /// </returns>
        /// <exception cref="OperationCanceledException">
        /// Thrown when the operation is canceled through <paramref name="cancellationToken"/>.<br/>
        /// <paramref name="cancellationToken"/>을 통해 작업이 취소된 경우 발생합니다.
        /// </exception>
        /// <remarks>
        /// A declined or canceled request is reported by returning <see langword="null"/>, not by throwing. Exceptions are reserved for actual failures.
        /// <br/><br/>
        /// 거부되거나 취소된 요청은 예외가 아니라 <see langword="null"/> 반환으로 알리며, 예외는 실제 오류에만 사용합니다.
        /// </remarks>
        UniTask<IIOProvider?> RequestRoot(CancellationToken cancellationToken = default);
    }
}
