#nullable enable
using RuniOS.NBS;
using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.NBS
{
    /// <summary>
    /// Draws the tempo, sound stopper, and file loop lanes below the timeline of a <see cref="NoteBlockSource"/>.<br/>
    /// <see cref="NoteBlockSource"/> 타임라인 아래에 템포, Sound Stopper, 파일 반복 레인을 그립니다.
    /// </summary>
    /// <remarks>
    /// Markers are merged per pixel column, so the cost depends on the lane width rather than the number of markers.
    /// Hovering a marker shows its details, and clicking it moves playback to it.<br/>
    /// 마커는 픽셀 열마다 합쳐지므로 비용은 마커 수가 아니라 레인 폭에 따라 정해집니다.
    /// 마커 위에 마우스를 올리면 정보를 표시하고, 클릭하면 재생 위치를 그 마커로 옮깁니다.
    /// </remarks>
    public static class NBSTimelineLaneDrawer
    {
        const float tempoLaneHeight = 14;
        const float stopperLaneHeight = 8;
        const float laneSpacing = 2;
        const float hoverDistance = 4;

        /// <summary>
        /// Gets the total height of the lanes.<br/>
        /// 레인 전체 높이를 가져옵니다.
        /// </summary>
        public const float height = tempoLaneHeight + laneSpacing + stopperLaneHeight;

        static Color laneBackground => EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.04f) : new Color(0, 0, 0, 0.06f);
        static Color tempoColor => EditorGUIUtility.isProSkin ? new Color(1, 0.72f, 0.25f, 0.85f) : new Color(0.85f, 0.5f, 0.05f, 0.9f);
        static Color tempoFill => EditorGUIUtility.isProSkin ? new Color(1, 0.72f, 0.25f, 0.18f) : new Color(0.85f, 0.5f, 0.05f, 0.2f);
        static Color stopperAllColor => EditorGUIUtility.isProSkin ? new Color(1, 0.3f, 0.3f, 1) : new Color(0.8f, 0.1f, 0.1f, 1);
        static Color stopperRangeColor => EditorGUIUtility.isProSkin ? new Color(1, 0.55f, 0.75f, 1) : new Color(0.75f, 0.2f, 0.5f, 1);
        static Color loopColor => EditorGUIUtility.isProSkin ? new Color(0.35f, 0.9f, 0.45f, 0.14f) : new Color(0.1f, 0.6f, 0.2f, 0.16f);
        static Color loopFlagColor => EditorGUIUtility.isProSkin ? new Color(0.35f, 0.9f, 0.45f, 0.9f) : new Color(0.1f, 0.55f, 0.2f, 0.9f);

        /// <summary>
        /// Draws the lanes for a timeline target. Matches the signature of <see cref="Sounds.PlaybackController.timelineLane"/>.<br/>
        /// 타임라인 대상의 레인을 그립니다. <see cref="Sounds.PlaybackController.timelineLane"/>의 시그니처와 같습니다.
        /// </summary>
        /// <param name="position">
        /// The lane area, horizontally aligned with the timeline track.<br/>
        /// 타임라인 트랙과 가로로 정렬된 레인 영역입니다.
        /// </param>
        /// <param name="seekable">
        /// The timeline target. Nothing is drawn unless it is a <see cref="NoteBlockSource"/> with a loaded clip.<br/>
        /// 타임라인 대상입니다. 클립이 로드된 <see cref="NoteBlockSource"/>가 아니면 아무것도 그리지 않습니다.
        /// </param>
        /// <param name="length">
        /// The timeline length in seconds.<br/>
        /// 초 단위 타임라인 길이입니다.
        /// </param>
        public static void Draw(Rect position, ISeekable seekable, double length)
        {
            if (seekable is not NoteBlockSource source || source.loadedClip == null || !(length > 0))
                return;

            NBSMap map = source.loadedClip.map;
            Rect tempoLane = new Rect(position.x, position.y, position.width, tempoLaneHeight);
            Rect stopperLane = new Rect(position.x, tempoLane.yMax + laneSpacing, position.width, stopperLaneHeight);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(tempoLane, laneBackground);
                EditorGUI.DrawRect(stopperLane, laneBackground);
                DrawFileLoop(position, source, source.loadedClip, length);
                DrawTempo(tempoLane, map, length);
                DrawStoppers(stopperLane, map, length);
            }

            HandleMarkerHover(tempoLane, source, map, length, true);
            HandleMarkerHover(stopperLane, source, map, length, false);
        }

        static float TimeToX(Rect lane, double time, double length) => lane.x + (float)(time / length * lane.width);

        static double XToTime(Rect lane, float x, double length) => (x - lane.x) / lane.width * length;

        static void DrawFileLoop(Rect position, NoteBlockSource source, NoteBlockClip clip, double length)
        {
            if (!source.loop || !source.useFileLoopSettings || !clip.header.loop)
                return;

            float start = TimeToX(position, Math.Clamp(clip.map.tempo.TickToTime(clip.header.loopStartTick), 0, length), length);
            float end = TimeToX(position, length, length);
            EditorGUI.DrawRect(new Rect(start, position.y, Math.Max(1, end - start), position.height), loopColor);
            EditorGUI.DrawRect(new Rect(start, position.y, 2, position.height), loopFlagColor);
            EditorGUI.DrawRect(new Rect(start, position.y, 6, 3), loopFlagColor);
        }

        static void DrawTempo(Rect lane, NBSMap map, double length)
        {
            ReadOnlySpan<NBSTempoChange> changes = map.tempo.changes.AsSpan();
            double minimum = double.PositiveInfinity;
            double maximum = double.NegativeInfinity;
            for (int i = 0; i < changes.Length; i++)
            {
                minimum = Math.Min(minimum, changes[i].ticksPerSecond);
                maximum = Math.Max(maximum, changes[i].ticksPerSecond);
            }

            int columns = Mathf.CeilToInt(lane.width);
            int changeIndex = 0;
            for (int x = 0; x < columns; x++)
            {
                double from = XToTime(lane, lane.x + x, length);
                double to = XToTime(lane, lane.x + x + 1, length);

                bool changed = false;
                while (changeIndex + 1 < changes.Length && changes[changeIndex + 1].time < to)
                {
                    changeIndex++;
                    changed |= changes[changeIndex].time >= from;
                }

                // The step graph uses the full lane height for the range of tempos in the song.
                double normalized = maximum > minimum ? (changes[changeIndex].ticksPerSecond - minimum) / (maximum - minimum) : 0.5;
                float barHeight = 2 + (float)(normalized * (lane.height - 3));
                EditorGUI.DrawRect(new Rect(lane.x + x, lane.yMax - barHeight, 1, barHeight), tempoFill);
                EditorGUI.DrawRect(new Rect(lane.x + x, lane.yMax - barHeight, 1, 1), tempoColor);

                if (changed && x > 0)
                    EditorGUI.DrawRect(new Rect(lane.x + x, lane.y, 1, lane.height), tempoColor);
            }
        }

        static void DrawStoppers(Rect lane, NBSMap map, double length)
        {
            ReadOnlySpan<NBSSoundStopper> stoppers = map.stoppers.AsSpan();
            if (stoppers.IsEmpty)
                return;

            int columns = Mathf.CeilToInt(lane.width);
            int index = 0;
            for (int x = 0; x < columns && index < stoppers.Length; x++)
            {
                double to = XToTime(lane, lane.x + x + 1, length);
                int count = 0;
                bool all = false;
                for (; index < stoppers.Length && stoppers[index].time < to; index++)
                {
                    count++;
                    all |= stoppers[index].startLayer == 0;
                }

                if (count == 0)
                    continue;

                // More stoppers in one pixel column draw a more opaque mark; stoppers for every layer are taller.
                Color color = all ? stopperAllColor : stopperRangeColor;
                color.a *= Math.Min(1, 0.55f + (0.15f * count));
                float markHeight = all ? lane.height : lane.height * 0.6f;
                EditorGUI.DrawRect(new Rect(lane.x + x - 0.5f, lane.yMax - markHeight, 2, markHeight), color);
            }
        }

        static void HandleMarkerHover(Rect lane, NoteBlockSource source, NBSMap map, double length, bool tempoLane)
        {
            Event current = Event.current;
            if (!lane.Contains(current.mousePosition) || (current.type != EventType.Repaint && current.type != EventType.MouseDown))
                return;

            double center = XToTime(lane, current.mousePosition.x, length);
            double radius = hoverDistance / lane.width * length;
            if (!(tempoLane ? TryFindTempoChange(map, center, radius, out string tooltip, out double time) : TryFindStopper(map, center, radius, out tooltip, out time)))
                return;

            if (current.type == EventType.MouseDown && current.button == 0)
            {
                source.time = time;
                current.Use();
                return;
            }

            GUI.Label(new Rect(current.mousePosition.x - hoverDistance, lane.y, hoverDistance * 2, lane.height), TempContent(string.Empty, tooltip));
        }

        static int LowerBound<T>(ReadOnlySpan<T> span, Func<T, double> getTime, double time)
        {
            int low = 0;
            int high = span.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (getTime(span[middle]) < time)
                    low = middle + 1;
                else
                    high = middle;
            }

            return low;
        }

        static bool TryFindTempoChange(NBSMap map, double center, double radius, out string tooltip, out double time)
        {
            ReadOnlySpan<NBSTempoChange> changes = map.tempo.changes.AsSpan();
            int nearest = -1;
            for (int i = Math.Max(1, LowerBound(changes, static x => x.time, center - radius)); i < changes.Length && changes[i].time <= center + radius; i++)
            {
                if (nearest < 0 || Math.Abs(changes[i].time - center) < Math.Abs(changes[nearest].time - center))
                    nearest = i;
            }

            if (nearest < 0)
            {
                tooltip = string.Empty;
                time = 0;
                return false;
            }

            NBSTempoChange change = changes[nearest];
            time = change.time;
            tooltip = $"BPM {changes[nearest - 1].beatsPerMinute:0.##} → {change.beatsPerMinute:0.##} · tick {change.tick} · {TimeUtility.ToTimeString(change.time)}";
            return true;
        }

        static bool TryFindStopper(NBSMap map, double center, double radius, out string tooltip, out double time)
        {
            ReadOnlySpan<NBSSoundStopper> stoppers = map.stoppers.AsSpan();
            int nearest = -1;
            for (int i = LowerBound(stoppers, static x => x.time, center - radius); i < stoppers.Length && stoppers[i].time <= center + radius; i++)
            {
                if (nearest < 0 || Math.Abs(stoppers[i].time - center) < Math.Abs(stoppers[nearest].time - center))
                    nearest = i;
            }

            if (nearest < 0)
            {
                tooltip = string.Empty;
                time = 0;
                return false;
            }

            NBSSoundStopper stopper = stoppers[nearest];
            time = stopper.time;
            string target = stopper.startLayer == 0
                ? GetTextOrKey("runios-editor:inspector.nbs_player.stopper.all_layers")
                : stopper.endLayer < stopper.startLayer
                    ? GetTextOrKey("runios-editor:inspector.nbs_player.stopper.no_layer")
                    : $"{GetTextOrKey("runios-editor:inspector.nbs_player.stopper.layers")} {stopper.startLayer}–{stopper.endLayer}";
            tooltip = $"Sound Stopper · {target} · tick {stopper.tick} · {TimeUtility.ToTimeString(stopper.time)}";
            return true;
        }
    }
}
