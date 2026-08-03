#nullable enable
namespace RuniOS.NBS
{
    /// <summary>
    /// Resolves persistent NBS visual-effect state and transient save-popup events by logical tick.<br/>
    /// 논리 틱을 기준으로 지속되는 NBS 시각 효과 상태와 일시적인 저장 팝업 이벤트를 확인합니다.
    /// </summary>
    public sealed class NBSVisualEffectMap
    {
        /// <summary>
        /// Stores the persistent visual-effect state active at a logical tick.<br/>
        /// 논리 틱에서 활성화된 지속 시각 효과 상태를 저장합니다.
        /// </summary>
        /// <param name="rainbowEnabled">
        /// Whether rainbow accent animation is enabled.<br/>
        /// 무지개 강조색 애니메이션의 활성화 여부입니다.
        /// </param>
        /// <param name="backgroundAccentEnabled">
        /// Whether the background uses the current accent color.<br/>
        /// 배경에서 현재 강조색을 사용하는지 여부입니다.
        /// </param>
        /// <param name="mainColor">
        /// The latest explicit main color, or <see langword="null"/> before the first color-change event.<br/>
        /// 마지막으로 명시된 주 색상이며, 첫 색상 변경 이벤트 이전이면 <see langword="null"/>입니다.
        /// </param>
        public readonly record struct State(bool rainbowEnabled, bool backgroundAccentEnabled, HexColor? mainColor);

        /// <summary>
        /// Describes a visual-effect state range beginning at <paramref name="startTick"/>.<br/>
        /// <paramref name="startTick"/>에서 시작하는 시각 효과 상태 구간을 설명합니다.
        /// </summary>
        /// <param name="startTick">
        /// The first tick at which <paramref name="state"/> is active.<br/>
        /// <paramref name="state"/>가 활성화되는 첫 틱입니다.
        /// </param>
        /// <param name="state">
        /// The persistent state active throughout the segment.<br/>
        /// 구간 전체에서 활성화되는 지속 상태입니다.
        /// </param>
        public readonly record struct Segment(int startTick, State state);

        /// <summary>
        /// Initializes a visual-effect map from parsed special events.<br/>
        /// 파싱된 특수 이벤트로 시각 효과 맵을 초기화합니다.
        /// </summary>
        /// <param name="events">
        /// The special events to precompute in tick and layer order.<br/>
        /// 틱과 레이어 순서로 미리 계산할 특수 이벤트입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="events"/> is <see langword="null"/>.<br/>
        /// <paramref name="events"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        public NBSVisualEffectMap(IEnumerable<NBSSpecialEvent> events)
        {
            if (events == null)
                throw new ArgumentNullException(nameof(events));

            NBSSpecialEvent[] visualEvents = events
                .Where(IsVisualEffect)
                .OrderBy(x => x.tick)
                .ThenBy(x => x.layer)
                .ToArray();

            List<Segment> stateSegments = [];
            List<int> popupTicks = [];
            State state = default;

            foreach (IGrouping<int, NBSSpecialEvent> tickEvents in visualEvents.GroupBy(x => x.tick))
            {
                State previousState = state;
                foreach (NBSSpecialEvent specialEvent in tickEvents)
                {
                    switch (specialEvent.kind)
                    {
                        case NBSSpecialEventKind.toggleRainbow:
                            state = state with { rainbowEnabled = !state.rainbowEnabled };
                            break;
                        case NBSSpecialEventKind.showSavePopup:
                            popupTicks.Add(specialEvent.tick);
                            break;
                        case NBSSpecialEventKind.toggleBackgroundAccent:
                            state = state with { backgroundAccentEnabled = !state.backgroundAccentEnabled };
                            break;
                        case NBSSpecialEventKind.changeMainColor:
                            state = state with { mainColor = specialEvent.color };
                            break;
                    }
                }

                if (state != previousState)
                    stateSegments.Add(new Segment(tickEvents.Key, state));
            }

            segments = stateSegments.AsReadOnly();
            savePopupTicks = Array.AsReadOnly(popupTicks.Distinct().ToArray());
            hasEvents = visualEvents.Length > 0;
        }

        /// <summary>
        /// Gets persistent-state segments ordered by their starting tick.<br/>
        /// 시작 틱 순서로 정렬된 지속 상태 구간을 가져옵니다.
        /// </summary>
        public IReadOnlyList<Segment> segments { get; }

        /// <summary>
        /// Gets distinct save-popup ticks in ascending order.<br/>
        /// 중복 없이 오름차순으로 정렬된 저장 팝업 틱을 가져옵니다.
        /// </summary>
        public IReadOnlyList<int> savePopupTicks { get; }

        /// <summary>
        /// Gets whether this map contains any persistent or transient visual effect.<br/>
        /// 이 맵에 지속 또는 일시 시각 효과가 포함되어 있는지 여부를 가져옵니다.
        /// </summary>
        public bool hasEvents { get; }

        /// <summary>
        /// Gets the persistent visual-effect state active at <paramref name="tick"/>.<br/>
        /// <paramref name="tick"/>에서 활성화된 지속 시각 효과 상태를 가져옵니다.
        /// </summary>
        /// <param name="tick">
        /// The logical tick to query.<br/>
        /// 조회할 논리 틱입니다.
        /// </param>
        /// <returns>
        /// The state after applying every persistent event at or before <paramref name="tick"/>.<br/>
        /// <paramref name="tick"/> 이하의 모든 지속 이벤트를 적용한 상태를 반환합니다.
        /// </returns>
        public State GetState(double tick)
        {
            if (segments.Count == 0 || double.IsNaN(tick) || tick < segments[0].startTick)
                return default;

            int low = 0;
            int high = segments.Count - 1;
            while (low < high)
            {
                int middle = (low + high + 1) / 2;
                if (segments[middle].startTick <= tick)
                    low = middle;
                else
                    high = middle - 1;
            }

            return segments[low].state;
        }

        /// <summary>
        /// Finds the first save-popup event crossed between two logical tick positions.<br/>
        /// 두 논리 틱 위치 사이에서 처음 통과한 저장 팝업 이벤트를 찾습니다.
        /// </summary>
        /// <param name="previousTick">
        /// The previous position, excluded from the search range.<br/>
        /// 검색 범위에서 제외되는 이전 위치입니다.
        /// </param>
        /// <param name="currentTick">
        /// The current position, included in the search range.<br/>
        /// 검색 범위에 포함되는 현재 위치입니다.
        /// </param>
        /// <param name="eventTick">
        /// The crossed event tick, or zero when no event was crossed.<br/>
        /// 통과한 이벤트 틱이며, 이벤트를 통과하지 않았으면 0입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an event was crossed in either direction; otherwise, <see langword="false"/>.<br/>
        /// 어느 방향에서든 이벤트를 통과했으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool TryGetSavePopupCrossing(double previousTick, double currentTick, out int eventTick)
        {
            eventTick = 0;
            if (savePopupTicks.Count == 0 || double.IsNaN(previousTick) || double.IsNaN(currentTick) || previousTick.Approximately(currentTick))
                return false;

            if (previousTick < currentTick)
            {
                int index = FindFirstSavePopupAfter(previousTick);
                if (index >= savePopupTicks.Count || savePopupTicks[index] > currentTick)
                    return false;

                eventTick = savePopupTicks[index];
                return true;
            }

            int reverseIndex = FindLastSavePopupBefore(previousTick);
            if (reverseIndex < 0 || savePopupTicks[reverseIndex] < currentTick)
                return false;

            eventTick = savePopupTicks[reverseIndex];
            return true;
        }

        static bool IsVisualEffect(NBSSpecialEvent specialEvent) => specialEvent.kind is
            NBSSpecialEventKind.toggleRainbow or
            NBSSpecialEventKind.showSavePopup or
            NBSSpecialEventKind.toggleBackgroundAccent or
            NBSSpecialEventKind.changeMainColor;

        int FindFirstSavePopupAfter(double tick)
        {
            int low = 0;
            int high = savePopupTicks.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (savePopupTicks[middle] <= tick)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        int FindLastSavePopupBefore(double tick)
        {
            int low = 0;
            int high = savePopupTicks.Count;
            while (low < high)
            {
                int middle = (low + high) / 2;
                if (savePopupTicks[middle] < tick)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low - 1;
        }
    }
}
