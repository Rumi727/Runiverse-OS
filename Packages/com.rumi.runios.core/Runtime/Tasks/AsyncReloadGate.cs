#nullable enable
using Cysharp.Threading.Tasks;

namespace RuniOS.Tasks
{
    /// <summary>
    /// Serializes asynchronous reload requests and merges pending requests into the latest reload pass.<br/>
    /// 비동기 리로드 요청을 직렬화하고 대기 중인 요청을 최신 리로드 패스로 병합합니다.
    /// </summary>
    public sealed class AsyncReloadGate
    {
        Func<IProgress<float>?, UniTask>? pendingReload;
        readonly List<IProgress<float>> pendingProgresses = [];
        UniTaskCompletionSource? completionSource;

        /// <summary>
        /// Gets whether a reload batch is currently running.<br/>
        /// 현재 리로드 배치가 실행 중인지 여부를 가져옵니다.
        /// </summary>
        public bool isRunning => completionSource != null;

        /// <summary>
        /// Requests a reload pass and joins the current reload batch if one is already running.<br/>
        /// 리로드 패스를 요청하고 이미 실행 중인 리로드 배치가 있으면 해당 배치에 참가합니다.
        /// </summary>
        /// <param name="reload">
        /// The reload operation to run on the latest pending pass.<br/>
        /// 최신 대기 패스에서 실행할 리로드 작업입니다.
        /// </param>
        /// <param name="progress">
        /// The optional progress receiver for the pass assigned to this request.<br/>
        /// 이 요청에 배정된 패스의 선택적 진행률 수신자입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that completes when the entire reload batch becomes idle.<br/>
        /// 전체 리로드 배치가 유휴 상태가 되면 완료되는 비동기 작업입니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="reload"/> is <see langword="null"/>.<br/>
        /// <paramref name="reload"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public UniTask Run
        (
            Func<IProgress<float>?, UniTask> reload,
            IProgress<float>? progress = null
        )
        {
            ExceptionUtility.ThrowIfArgumentNull(reload, nameof(reload));

            pendingReload = reload;
            AddPendingProgress(progress);

            if (completionSource != null)
                return completionSource.Task;

            UniTaskCompletionSource source = new();
            completionSource = source;

            RunLoop(source).Forget();
            return source.Task;
        }

        /// <summary>
        /// Requests a reload pass that does not report progress.<br/>
        /// 진행률을 보고하지 않는 리로드 패스를 요청합니다.
        /// </summary>
        /// <param name="reload">
        /// The reload operation to run on the latest pending pass.<br/>
        /// 최신 대기 패스에서 실행할 리로드 작업입니다.
        /// </param>
        /// <returns>
        /// An asynchronous operation that completes when the entire reload batch becomes idle.<br/>
        /// 전체 리로드 배치가 유휴 상태가 되면 완료되는 비동기 작업입니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="reload"/> is <see langword="null"/>.<br/>
        /// <paramref name="reload"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public UniTask Run(Func<UniTask> reload)
        {
            ExceptionUtility.ThrowIfArgumentNull(reload, nameof(reload));
            return Run(_ => reload());
        }

        void AddPendingProgress(IProgress<float>? progress)
        {
            if (progress == null)
                return;

            if (pendingProgresses.Any(pendingProgress => ReferenceEquals(pendingProgress, progress)))
                return;

            pendingProgresses.Add(progress);
        }

        async UniTask RunLoop(UniTaskCompletionSource source)
        {
            List<Exception>? exceptions = null;

            try
            {
                do
                {
                    Func<IProgress<float>?, UniTask> reload = pendingReload!;
                    pendingReload = null;

                    IProgress<float>[] progresses = pendingProgresses.ToArray();
                    pendingProgresses.Clear();

                    IProgress<float>? combinedProgress = CreateCombinedProgress(progresses);

                    try
                    {
                        await reload.Invoke(combinedProgress);
                    }
                    catch (Exception exception)
                    {
                        (exceptions ??= []).Add(exception);
                    }
                }
                while (pendingReload != null);
            }
            catch (Exception exception)
            {
                (exceptions ??= []).Add(exception);
            }
            finally
            {
                pendingReload = null;
                pendingProgresses.Clear();
                completionSource = null;
            }

            if (exceptions == null)
                source.TrySetResult();
            else if (exceptions.Count == 1)
                source.TrySetException(exceptions[0]);
            else
                source.TrySetException(new AggregateException(exceptions));
        }

        static IProgress<float>? CreateCombinedProgress(IProgress<float>[] progresses)
        {
            return progresses.Length switch
            {
                0 => null,
                1 => progresses[0],
                _ => new CombinedProgress(progresses)
            };
        }

        sealed class CombinedProgress(IProgress<float>[] progresses) : IProgress<float>
        {
            public void Report(float value)
            {
                foreach (IProgress<float> progress in progresses)
                    progress.SafeReport(value);
            }
        }
    }
}