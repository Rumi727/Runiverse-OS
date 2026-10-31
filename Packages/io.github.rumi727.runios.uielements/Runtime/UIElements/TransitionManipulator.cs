#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// target에서 실행 중인 USS 전환을 추적합니다. 자식에서 버블된 전환 이벤트는 세지 않습니다.
    /// </summary>
    public class TransitionManipulator : Manipulator
    {
        public TransitionManipulator() { }
        public TransitionManipulator(Action callback) => onTransitionsEnded = callback;

        /// <summary>
        /// 실행 중인 전환이 모두 끝나거나 취소되어 0개가 되었을 때 발생합니다.
        /// </summary>
        public event Action? onTransitionsEnded;

        public bool isRunning => runningProperties.Count > 0;

        readonly HashSet<StylePropertyName> runningProperties = [];
        readonly HashSet<UniTaskCompletionSource> waits = [];

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<TransitionRunEvent>(OnTransitionRun);
            target.RegisterCallback<TransitionEndEvent>(OnTransitionEnd);
            target.RegisterCallback<TransitionCancelEvent>(OnTransitionCancel);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<TransitionRunEvent>(OnTransitionRun);
            target.UnregisterCallback<TransitionEndEvent>(OnTransitionEnd);
            target.UnregisterCallback<TransitionCancelEvent>(OnTransitionCancel);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetach);

            runningProperties.Clear();
            CompleteWaits();
        }

        /// <summary>
        /// 직전 스타일 변경으로 생긴 전환이 모두 끝나면 완료됩니다.
        /// 전환이 없거나 target이 패널에 없으면 즉시 완료됩니다.
        /// </summary>
        public async UniTask WaitForTransitions(CancellationToken cancellationToken = default)
        {
            if (target.panel is not { } panel)
                return;

            // 전환 이벤트는 스타일 갱신 다음 프레임에야 디스패치되므로, 바뀐 스타일을 지금 바로 계산해 전환 설정을 읽습니다.
            // Pick은 정확한 결과를 위해 스타일과 레이아웃을 먼저 갱신합니다. 문서화된 동작은 아니며,
            // 스타일·레이아웃 갱신 중의 콜백에서 호출하면 재진입 방지로 갱신되지 않아 이전 스타일 기준으로 계산됩니다.
            panel.Pick(Vector2.zero);

            int durationMs = GetMaxTransitionTimeMs(target.resolvedStyle);
            if (durationMs <= 0)
                return;

            // 전환 설정의 최대 시간이 지나고 실행 중인 전환도 없어야 끝납니다.
            // 최대 시간은 전환 이벤트가 오기 전의 공백을 메우고, 실행 중 확인은 끝 이벤트가 한 프레임 늦게 오는 경우를 기다립니다.
            UniTaskCompletionSource source = new();
            waits.Add(source);

            IVisualElementScheduledItem item = target.schedule.Execute(() =>
            {
                if (!isRunning)
                    source.TrySetResult();
            }).StartingIn(durationMs).Every(0).Until(() => source.Task.Status != UniTaskStatus.Pending);

            try
            {
                using (cancellationToken.Register(() => source.TrySetCanceled(cancellationToken)))
                    await source.Task;
            }
            finally
            {
                item.Pause();
                waits.Remove(source);
            }
        }

        // ComputedTransitionUtils와 같은 규칙: transition-property가 기준 목록이고, 짧은 목록은 순환합니다.
        static int GetMaxTransitionTimeMs(IResolvedStyle style)
        {
            int count = style.transitionProperty.Count();
            if (count == 0)
                return 0;

            TimeValue[] durations = style.transitionDuration.ToArray();
            TimeValue[] delays = style.transitionDelay.ToArray();

            int max = 0;
            for (int i = 0; i < count; i++)
            {
                int duration = durations.Length == 0 ? 0 : ToMilliseconds(durations[i % durations.Length]);
                int delay = delays.Length == 0 ? 0 : ToMilliseconds(delays[i % delays.Length]);

                max = Math.Max(max, Math.Max(0, duration) + delay);
            }

            return max;

            static int ToMilliseconds(TimeValue time) => Mathf.RoundToInt(time.unit == TimeUnit.Millisecond ? time.value : time.value * 1000);
        }

        void OnTransitionRun(TransitionRunEvent evt)
        {
            if (evt.target != target)
                return;

            foreach (StylePropertyName name in evt.stylePropertyNames)
                runningProperties.Add(name);
        }

        void OnTransitionEnd(TransitionEndEvent evt) => RemoveRunning(evt);

        void OnTransitionCancel(TransitionCancelEvent evt) => RemoveRunning(evt);

        void RemoveRunning(ITransitionEvent evt)
        {
            if (((EventBase)evt).target != target || runningProperties.Count == 0)
                return;

            foreach (StylePropertyName name in evt.stylePropertyNames)
                runningProperties.Remove(name);

            if (runningProperties.Count == 0)
                OnTransitionsEnded();
        }

        // 전환은 패널에 붙어 있을 때만 실행되므로, 패널에서 떨어지면 모두 끝난 것으로 봅니다.
        void OnDetach(DetachFromPanelEvent evt)
        {
            bool wasRunning = isRunning;

            runningProperties.Clear();
            CompleteWaits();

            if (wasRunning)
                OnTransitionsEnded();
        }

        void CompleteWaits()
        {
            foreach (UniTaskCompletionSource source in waits.ToArray())
                source.TrySetResult();
        }

        protected virtual void OnTransitionsEnded() => onTransitionsEnded?.Invoke();
    }
}
