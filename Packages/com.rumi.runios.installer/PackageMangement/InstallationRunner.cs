#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RuniOS.PackageManagement
{
    /// <summary>
    /// Matches caller-supplied executors and invokes each matching batch once.<br/>
    /// caller가 제공한 executor를 matching하고 matching된 batch별로 한 번 호출합니다.
    /// </summary>
    /// <remarks>
    /// Fully enumerating the stream runs all matching executor entries; composition safety and executor ordering are not guaranteed.<br/>
    /// Results are accounted for per input entry using default descriptor equality.<br/>
    /// Exceptions from executor enumeration, including disposal, propagate to the caller.
    /// <br/><br/>
    /// stream을 끝까지 열거하면 matching된 모든 executor 항목을 실행하며 조합 안전성과 executor 순서는 보장하지 않습니다.<br/>
    /// descriptor의 기본 동등성으로 입력 항목별 결과를 추적합니다.<br/>
    /// 해제를 포함한 executor 열거의 예외는 호출자에게 전파합니다.
    /// </remarks>
    public sealed class InstallationRunner
    {
        sealed class ExecutorBatch
        {
            internal readonly IInstallationExecutor executor;
            internal readonly IReadOnlyList<IInstallation> installations;
            internal ExecutorBatch(IInstallationExecutor executor, List<IInstallation> installations)
            { this.executor = executor; this.installations = installations.AsReadOnly(); }
        }
        sealed class DispatchResult
        {
            internal readonly IReadOnlyList<ExecutorBatch> batches;
            internal readonly IReadOnlyList<IInstallation> unsupported;
            internal DispatchResult(List<ExecutorBatch> batches, List<IInstallation> unsupported)
            { this.batches = batches.AsReadOnly(); this.unsupported = unsupported.AsReadOnly(); }
        }
        readonly IInstallationExecutor[] _executors;
        /// <summary>
        /// Retains executor references without taking ownership of their lifetime.<br/>
        /// executor 수명을 소유하지 않고 참조를 보관합니다.
        /// </summary>
        /// <param name="executors">
        /// The executor instances configured by the caller.<br/>
        /// caller가 구성한 executor 인스턴스들입니다.
        /// </param>
        /// <exception cref="ArgumentException">
        /// Thrown when an executor entry is null.<br/>
        /// executor 항목이 null이면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when executors is null.<br/>
        /// executors가 null이면 발생합니다.
        /// </exception>
        public InstallationRunner(IEnumerable<IInstallationExecutor> executors)
        {
            _executors = (executors ?? throw new ArgumentNullException(nameof(executors))).ToArray();
            foreach (IInstallationExecutor executor in _executors)
                if (executor is null) throw new ArgumentException("An executor cannot be null.", nameof(executors));
        }
        /// <summary>
        /// Classifies descriptors once and streams outcomes from every matching executor.<br/>
        /// descriptor를 한 번 분류하고 모든 matching executor의 결과를 비동기로 열거합니다.
        /// </summary>
        /// <param name="installations">
        /// The descriptors to ensure.<br/>
        /// 요구사항을 만족시킬 descriptor들입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel enumeration and execution.<br/>
        /// 열거와 실행 취소에 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// One result per executor and input entry, or an unsupported result when no executor matches.<br/>
        /// executor와 입력 항목별 결과 또는 matching이 없을 때 지원되지 않는다는 결과를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an installation entry is null.<br/>
        /// installation 항목이 null이면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when installations is null.<br/>
        /// installations가 null이면 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when execution is cancelled.<br/>
        /// 실행이 취소되면 발생합니다.
        /// </exception>
        public async IAsyncEnumerable<InstallationResult> EnsureAsync(IEnumerable<IInstallation> installations, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            DispatchResult dispatch = Dispatch(installations, cancellationToken);
            await foreach (InstallationResult result in ExecuteAsync(dispatch, cancellationToken).ConfigureAwait(false))
                yield return result;
        }
        /// <summary>
        /// Independently groups descriptors and observes every matching executor's optional preview capability.<br/>
        /// descriptor를 독립적으로 grouping하고 모든 matching executor의 선택적인 preview capability를 관측합니다.
        /// </summary>
        /// <param name="installations">
        /// The descriptors to observe.<br/>
        /// 관측할 descriptor들입니다.
        /// </param>
        /// <param name="cancellationToken">
        /// The token used to cancel enumeration and observation.<br/>
        /// 열거와 관측 취소에 사용하는 토큰입니다.
        /// </param>
        /// <returns>
        /// Advisory results per executor and input entry, including unsupported inputs and unavailable preview capabilities.<br/>
        /// 지원되지 않는 입력과 preview capability 부재를 포함한 executor 및 입력 항목별 advisory 결과를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when an installation entry is <see langword="null"/>.<br/>
        /// installation 항목이 <see langword="null"/>이면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="installations"/> is <see langword="null"/>.<br/>
        /// <paramref name="installations"/>가 <see langword="null"/>이면 발생합니다.
        /// </exception>
        /// <exception cref="OperationCanceledException">
        /// Thrown when observation is cancelled.<br/>
        /// 관측이 취소되면 발생합니다.
        /// </exception>
        public async IAsyncEnumerable<InstallationPreview> PreviewAsync(IEnumerable<IInstallation> installations, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            DispatchResult dispatch = Dispatch(installations, cancellationToken);
            await foreach (InstallationPreview preview in ObserveAsync(dispatch, cancellationToken).ConfigureAwait(false))
                yield return preview;
        }
        static async IAsyncEnumerable<InstallationPreview> ObserveAsync(DispatchResult dispatch, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (IInstallation installation in dispatch.unsupported)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new InstallationPreview(installation, InstallationPreviewStatus.Failed, new[] { new InstallationDiagnostic("installation:unsupported", "No configured executor supports this installation.") });
            }
            foreach (ExecutorBatch batch in dispatch.batches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IInstallationExecutor executor = batch.executor;
                if (executor is not IInstallationPreviewer previewer)
                {
                    foreach (IInstallation installation in batch.installations)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        yield return new InstallationPreview(installation, InstallationPreviewStatus.NotSupported, executor: executor);
                    }
                    continue;
                }
                var remaining = new List<IInstallation>(batch.installations);
                IAsyncEnumerator<InstallationPreview> enumerator = previewer.PreviewAsync(batch.installations, cancellationToken).GetAsyncEnumerator(cancellationToken);
                string? failureMessage = null;
                try
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) break;
                        InstallationPreview? preview = enumerator.Current;
                        if (preview is null) { failureMessage = "A previewer returned a null result."; break; }
                        cancellationToken.ThrowIfCancellationRequested();
                        remaining.Remove(preview.installation);
                        yield return new InstallationPreview(preview, executor);
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
                foreach (IInstallation installation in remaining)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new InstallationPreview(installation, InstallationPreviewStatus.Failed, new[]
                    {
                        new InstallationDiagnostic("installation:preview-failed", failureMessage ?? "The previewer did not return an observation for this installation.")
                    }, executor);
                }
            }
        }
        static async IAsyncEnumerable<InstallationResult> ExecuteAsync(DispatchResult dispatch, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (IInstallation installation in dispatch.unsupported)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return new InstallationResult(installation, false, new[] { new InstallationDiagnostic("installation:unsupported", "No configured executor supports this installation.") });
            }
            foreach (ExecutorBatch batch in dispatch.batches)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IInstallationExecutor executor = batch.executor;
                var remaining = new List<IInstallation>(batch.installations);
                IAsyncEnumerator<InstallationResult> enumerator = executor.EnsureAsync(batch.installations, cancellationToken).GetAsyncEnumerator(cancellationToken);
                string? failureMessage = null;
                try
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!await enumerator.MoveNextAsync().ConfigureAwait(false)) break;
                        InstallationResult? result = enumerator.Current;
                        if (result is null) { failureMessage = "An executor returned a null result."; break; }
                        cancellationToken.ThrowIfCancellationRequested();
                        remaining.Remove(result.installation);
                        yield return new InstallationResult(result, executor);
                    }
                }
                finally
                {
                    await enumerator.DisposeAsync().ConfigureAwait(false);
                }
                foreach (IInstallation installation in remaining)
                {
                    yield return new InstallationResult(installation, false, new[]
                    {
                        new InstallationDiagnostic("installation:executor-failed", failureMessage ?? "The executor did not return a final result for this installation.")
                    }, executor);
                }
            }
        }
        DispatchResult Dispatch(IEnumerable<IInstallation> installations, CancellationToken cancellationToken)
        {
            if (installations is null) throw new ArgumentNullException(nameof(installations));
            var batches = new List<IInstallation>?[_executors.Length];
            var unmatched = new List<IInstallation>();
            foreach (IInstallation installation in installations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (installation is null) throw new ArgumentException("An installation cannot be null.", nameof(installations));
                bool matched = false;
                for (int i = 0; i < _executors.Length; i++)
                {
                    if (!_executors[i].CanExecute(installation)) continue;
                    (batches[i] ??= new List<IInstallation>()).Add(installation);
                    matched = true;
                }
                if (!matched) unmatched.Add(installation);
            }
            var groups = new List<ExecutorBatch>();
            for (int i = 0; i < batches.Length; i++)
                if (batches[i] is { } installationsForExecutor)
                    groups.Add(new ExecutorBatch(_executors[i], installationsForExecutor));
            return new DispatchResult(groups, unmatched);
        }
    }
}
