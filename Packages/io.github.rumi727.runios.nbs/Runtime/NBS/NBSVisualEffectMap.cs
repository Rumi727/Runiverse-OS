#nullable enable
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace RuniOS.NBS
{
    /// <summary>
    /// Resolves the visual effects that Note Block Studio applies while the marker passes functional instruments.<br/>
    /// 마커가 기능성 악기를 지날 때 Note Block Studio가 적용하는 시각 효과를 확인합니다.
    /// </summary>
    /// <remarks>
    /// The map is immutable and can be read from any thread. Effects apply regardless of the layer lock, as in Note Block Studio.<br/>
    /// 맵은 불변이며 어느 스레드에서든 읽을 수 있습니다. Note Block Studio와 같이 효과는 레이어 잠금과 관계없이 적용됩니다.
    /// </remarks>
    public sealed class NBSVisualEffectMap
    {
        /// <summary>
        /// Initializes a visual effect map from notes sorted by tick and then by layer.<br/>
        /// tick, 레이어 순으로 정렬된 노트로 시각 효과 맵을 초기화합니다.
        /// </summary>
        /// <param name="sortedNotes">
        /// The notes in ascending tick and layer order.<br/>
        /// tick, 레이어 오름차순의 노트입니다.
        /// </param>
        /// <param name="instruments">
        /// The custom instruments that the note instrument indexes refer to.<br/>
        /// 노트 악기 인덱스가 가리키는 커스텀 악기입니다.
        /// </param>
        public NBSVisualEffectMap(ReadOnlySpan<NBSMapNote> sortedNotes, ReadOnlySpan<NBSInstrument> instruments)
        {
            NBSInstrumentFunction[] functions = new NBSInstrumentFunction[instruments.Length];
            Color32?[] colors = new Color32?[instruments.Length];
            bool hasEffect = false;
            for (int i = 0; i < instruments.Length; i++)
            {
                NBSInstrumentFunction function = instruments[i].function;
                functions[i] = function;
                if (function == NBSInstrumentFunction.changeColor && instruments[i].TryGetColor(out Color32 color))
                    colors[i] = color;

                hasEffect |= function is NBSInstrumentFunction.toggleRainbow or NBSInstrumentFunction.toggleBackgroundAccent or NBSInstrumentFunction.changeColor or NBSInstrumentFunction.showSavePopup;
            }

            if (!hasEffect)
            {
                states = ImmutableArray<NBSVisualEffectState>.Empty;
                savePopupTicks = ImmutableArray<int>.Empty;
                return;
            }

            List<NBSVisualEffectState> stateList = [];
            List<int> popupList = [];
            NBSVisualEffectState state = default;

            for (int i = 0; i < sortedNotes.Length; i++)
            {
                int customIndex = sortedNotes[i].note.instrument - NBSInstrument.vanillaCount;
                if ((uint)customIndex >= (uint)functions.Length)
                    continue;

                int tick = sortedNotes[i].tick;
                switch (functions[customIndex])
                {
                    case NBSInstrumentFunction.toggleRainbow:
                        state = state with { rainbow = !state.rainbow };
                        break;
                    case NBSInstrumentFunction.toggleBackgroundAccent:
                        state = state with { backgroundAccent = !state.backgroundAccent };
                        break;
                    case NBSInstrumentFunction.changeColor when colors[customIndex] != null:
                        state = state with { color = colors[customIndex] };
                        break;
                    case NBSInstrumentFunction.showSavePopup:
                        if (popupList.Count == 0 || popupList[^1] != tick)
                            popupList.Add(tick);
                        continue;
                    default:
                        continue;
                }

                state = state with { tick = tick };
                if (stateList.Count > 0 && stateList[^1].tick == tick)
                    stateList[^1] = state;
                else
                    stateList.Add(state);
            }

            states = ImmutableCollectionsMarshal.AsImmutableArray(stateList.ToArray());
            savePopupTicks = ImmutableCollectionsMarshal.AsImmutableArray(popupList.ToArray());
        }

        /// <summary>
        /// Gets the states in ascending tick order. Each state applies until the next one.<br/>
        /// 상태를 tick 오름차순으로 가져옵니다. 각 상태는 다음 상태 전까지 적용됩니다.
        /// </summary>
        public ImmutableArray<NBSVisualEffectState> states { get; }

        /// <summary>
        /// Gets the distinct ticks that show the save popup, in ascending order.<br/>
        /// 저장 팝업을 표시하는 tick을 중복 없이 오름차순으로 가져옵니다.
        /// </summary>
        public ImmutableArray<int> savePopupTicks { get; }

        /// <summary>
        /// Gets the state active at a tick position.<br/>
        /// tick 위치에서 활성화된 상태를 가져옵니다.
        /// </summary>
        /// <param name="tick">
        /// The tick position to query.<br/>
        /// 조회할 tick 위치입니다.
        /// </param>
        /// <returns>
        /// The state after every effect at or before <paramref name="tick"/>; <see langword="default"/> before the first effect.<br/>
        /// <paramref name="tick"/> 이하의 모든 효과를 적용한 상태를 반환하며, 첫 효과 이전이면 <see langword="default"/>를 반환합니다.
        /// </returns>
        public NBSVisualEffectState GetState(double tick)
        {
            ReadOnlySpan<NBSVisualEffectState> span = states.AsSpan();
            int low = 0;
            int high = span.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (span[middle].tick <= tick)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low > 0 ? span[low - 1] : default;
        }

        /// <summary>
        /// Finds the first save popup crossed when moving from one tick position to another, in either direction.<br/>
        /// 한 tick 위치에서 다른 위치로 이동할 때 어느 방향이든 처음 지나는 저장 팝업을 찾습니다.
        /// </summary>
        /// <param name="fromTick">
        /// The previous position, which is excluded.<br/>
        /// 제외되는 이전 위치입니다.
        /// </param>
        /// <param name="toTick">
        /// The current position, which is included.<br/>
        /// 포함되는 현재 위치입니다.
        /// </param>
        /// <param name="tick">
        /// The crossed popup tick when found; otherwise, <c>0</c>.<br/>
        /// 찾으면 지나간 팝업 tick이고, 그렇지 않으면 <c>0</c>입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if a popup was crossed; otherwise, <see langword="false"/>.<br/>
        /// 팝업을 지났으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool TryFindSavePopup(double fromTick, double toTick, out int tick)
        {
            ReadOnlySpan<int> span = savePopupTicks.AsSpan();
            tick = 0;
            if (fromTick.Approximately(toTick) || span.IsEmpty)
                return false;

            if (fromTick < toTick)
            {
                int index = UpperBound(span, fromTick);
                if (index < span.Length && span[index] <= toTick)
                {
                    tick = span[index];
                    return true;
                }
            }
            else
            {
                int index = LowerBound(span, fromTick) - 1;
                if (index >= 0 && span[index] >= toTick)
                {
                    tick = span[index];
                    return true;
                }
            }

            return false;
        }

        static int UpperBound(ReadOnlySpan<int> span, double value)
        {
            int low = 0;
            int high = span.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (span[middle] <= value)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        static int LowerBound(ReadOnlySpan<int> span, double value)
        {
            int low = 0;
            int high = span.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (span[middle] < value)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }
    }
}
