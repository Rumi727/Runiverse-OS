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
        /// <br/>배칭(Batching)으로 컨텍스트 스위칭 비용을 줄이고, 세마포어를 통해 배압(Backpressure)을 구현하여 메모리 낭비를 방지합니다.
        /// </summary>
        /// <param name="enumerable">순회할 소스 컬렉션입니다.</param>
        /// <param name="batchSize">한 번에 전달할 아이템의 개수입니다. (기본값: 64)</param>
        /// <param name="queueLimit">메모리에 미리 로드해둘 배치 덩어리의 개수입니다. (기본값: 2)</param>
        public static IUniTaskAsyncEnumerable<TSource> EnumerateOnThreadPool<TSource>
        (
            this IEnumerable<TSource> enumerable,
            int batchSize = 64,
            int queueLimit = 2
        ) => UniTaskAsyncEnumerable.Create<TSource>(async (writer, cancellationToken) =>
        {
            // 호출 시점의 컨텍스트(주로 Main Thread) 캡처
            SynchronizationContext? callerContext = SynchronizationContext.Current;
            
            // 데이터를 묶음으로 전달하는 채널 생성
            Channel<List<TSource>>? channel = Channel.CreateSingleConsumerUnbounded<List<TSource>>();
            
            // 배압(Backpressure)을 위한 세마포어 생성
            SemaphoreSlim semaphore = new SemaphoreSlim(queueLimit, queueLimit);

            // [Producer] 스레드 풀
            UniTask producerTask = UniTask.RunOnThreadPool(async () =>
            {
                try
                {
                    // 배칭을 위한 임시 버퍼 생성
                    List<TSource> buffer = new(batchSize);
                    foreach (var item in enumerable)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            return;

                        buffer.Add(item);

                        // 버퍼가 가득 차면 채널로 전송하고 새 버퍼 생성
                        if (buffer.Count >= batchSize)
                        {
                            // [3] 채널에 넣기 전에 세마포어 대기 (티켓 확인)
                            // 꽉 찼다면(티켓 0장), 소비자가 Release 할 때까지 여기서 멈춤 -> I/O 중단
                            // ReSharper disable once AccessToDisposedClosure
                            await semaphore.WaitAsync(cancellationToken);

                            // Unbounded지만 세마포어 때문에 사실상 Bounded처럼 동작함
                            channel.Writer.TryWrite(buffer);

                            // 중요: Clear()를 사용하여 리스트를 재사용하면 안 됩니다.
                            // List<T>는 참조 타입이므로, 채널로 넘긴 리스트를 Consumer가 아직 읽고 있을 수 있습니다.
                            // 이때 Clear를 하면 데이터 경합(Race Condition)이 발생하므로, 반드시 새 인스턴스를 생성해야 합니다.
                            buffer = new List<TSource>(batchSize);
                        }
                    }

                    // 남은 데이터가 있다면 전송
                    if (buffer.Count > 0)
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        await semaphore.WaitAsync(cancellationToken);
                        channel.Writer.TryWrite(buffer);
                    }
                }
                catch (Exception exception)
                {
                    channel.Writer.TryComplete(exception);
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, cancellationToken: cancellationToken);

            // [Consumer] 호출한 컨텍스트(Main Thread)로 복귀하여 실행
            try
            {
                // 채널에서 '묶음(batch)' 단위로 데이터를 가져옴 (여기서 컨텍스트 스위칭 발생)
                await foreach (var batch in channel.Reader.ReadAllAsync(cancellationToken))
                {
                    // 데이터를 받으면 원래 컨텍스트로 스위칭
                    if (callerContext != null && SynchronizationContext.Current != callerContext)
                        await UniTask.SwitchToSynchronizationContext(callerContext);

                    foreach (var item in batch)
                        await writer.YieldAsync(item);

                    // [4] 소비 완료 후 세마포어 반납 (티켓 반환)
                    // 이제 Producer가 다음 배치를 넣을 수 있게 됨
                    semaphore.Release();
                }

                // 루프 종료 후 마지막으로 컨텍스트 보장
                if (callerContext != null && SynchronizationContext.Current != callerContext)
                    await UniTask.SwitchToSynchronizationContext(callerContext);
            }
            catch (OperationCanceledException)
            {
                // 취소 시 예외 무시
            }
            finally
            {
                // Producer 스레드가 완전히 종료될 때까지 기다립니다.
                // 이 줄이 통과되어야 아래 Dispose가 실행되므로, 'AccessToDisposedClosure' 문제는 절대 발생하지 않습니다.
                await producerTask.SuppressCancellationThrow();
                semaphore.Dispose();
            }
        });
    }
}