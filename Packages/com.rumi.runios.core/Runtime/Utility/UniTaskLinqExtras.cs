#nullable enable
using Cysharp.Threading.Tasks.Linq;
using Cysharp.Threading.Tasks;
using System.Threading;

// ReSharper disable once CheckNamespace
namespace RuniOS.Linq.Async
{
    public static class UniTaskLinqExtras
    {
        /// <summary>
        /// 동기 IEnumerable을 스레드 풀에서 실행되는 비동기 스트림으로 변환합니다.
        /// Producer-Consumer 패턴을 사용하여 I/O 병목을 최소화합니다.
        /// </summary>
        public static IUniTaskAsyncEnumerable<TSource> EnumerateOnThreadPool<TSource>(this IEnumerable<TSource> enumerable) => UniTaskAsyncEnumerable.Create<TSource>(async (writer, cancellationToken) =>
        {
            SynchronizationContext? callerContext = SynchronizationContext.Current;
            
            Channel<TSource>? channel = Channel.CreateSingleConsumerUnbounded<TSource>();
            UniTask.RunOnThreadPool(() =>
            {
                try
                {
                    foreach (var item in enumerable)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        if (!channel.Writer.TryWrite(item))
                            return;
                    }
                }
                catch (Exception exception)
                {
                    channel.Writer.Complete(exception);
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, cancellationToken: cancellationToken)
            .SuppressCancellationThrow()
            .Forget();

            try
            {
                await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    if (callerContext != null && SynchronizationContext.Current != callerContext)
                        await UniTask.SwitchToSynchronizationContext(callerContext);

                    await writer.YieldAsync(item);
                }

                if (callerContext != null && SynchronizationContext.Current != callerContext)
                    await UniTask.SwitchToSynchronizationContext(callerContext);
            }
            catch (OperationCanceledException)
            {

            }
        });
    }
}