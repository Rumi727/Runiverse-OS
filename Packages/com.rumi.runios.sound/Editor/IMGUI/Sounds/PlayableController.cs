#nullable enable
using RuniOS.Sounds;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public sealed class PlayableController
    {
        enum LoopHandle
        {
            none,
            start,
            end
        }

        sealed class TimeUnitContents
        {
            public readonly GUIContent time = new GUIContent();
            public readonly GUIContent remainingTime = new GUIContent();
            public readonly GUIContent length = new GUIContent();
            public bool initialized;
        }

        static readonly int loopRangeHash = "PlayableControllerLoopRange".GetHashCode();

        public PlayableController() => timeUnits = [];
        public PlayableController(params ITimeUnit[] timeUnits) => this.timeUnits = timeUnits.ToList();
        public PlayableController(IEnumerable<ITimeUnit> timeUnits) => this.timeUnits = timeUnits.ToList();

        public List<IPlayable> targets { get; } = [];

        [DisallowNull]
        public IPlayable? target
        {
            get => targets.Count == 1 ? targets[0] : null;
            set
            {
                targets.Clear();
                targets.Add(value);
            }
        }

        public List<ITimeUnit> timeUnits { get; }

        public Action<ILoopablePlayer, double, double>? loopRangeSetter { private get; set; }

        LoopHandle draggedLoopHandle;
        readonly Vector3[] startHandleVertices = new Vector3[3];
        readonly Vector3[] endHandleVertices = new Vector3[3];
        readonly List<TimeUnitContents> timeUnitContents = [];

        public void DrawLayout() => Draw(EditorGUILayout.GetControlRect(false, GetHeight()));

        public void Draw(Rect position)
        {
            float orgWidth = position.width;
            position.width = 150;

            DrawControl(position);

            position.x += position.width + 8;
            position.width = orgWidth - (position.width + 8);

            DrawSlider(position);
        }

        public void DrawControl(Rect position)
        {
            float orgX = position.x;
            float orgWidth = position.width;

            position.height = GetYSize(GUI.skin.button);
            position.width = (orgWidth - (3 * 2)) / 3;

            bool play = false;
            bool anyPlaying = false;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].isPlaying)
                {
                    anyPlaying = true;
                    break;
                }
            }

            if (anyPlaying)
            {
                if (GUI.Button(position, "▶↻"))
                    play = true;
            }
            else if (GUI.Button(position, "▶"))
                play = true;

            if (play)
            {
                foreach (var target in targets)
                    target.Play();
            }

            position.x += position.width + 3;

            bool allPaused = true;
            for (int i = 0; i < targets.Count; i++)
            {
                if (!targets[i].isPaused)
                {
                    allPaused = false;
                    break;
                }
            }

            if (allPaused)
            {
                if (GUI.Button(position, "▶▮"))
                {
                    foreach (var target in targets)
                        target.UnPause();
                }
            }
            else if (GUI.Button(position, "▮▮"))
            {
                foreach (var target in targets)
                    target.Pause();
            }

            position.x += position.width + 3;

            if (GUI.Button(position, "■"))
            {
                foreach (var target in targets)
                    target.Stop();
            }

            position.x = orgX;
            position.width = orgWidth;

            position.y += position.height;
            position.y += EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.BeginDisabledGroup(target == null || !target.isPlaying);

            BeginLabelWidth(75);
            for (int i = 0; i < timeUnits.Count; i++)
            {
                ITimeUnit timeUnit = timeUnits[i];
                position.height = timeUnit.GetHeight();

                timeUnit.DrawField(position, targets);

                position.y += position.height;
                position.y += EditorGUIUtility.standardVerticalSpacing;
            }
            EndLabelWidth();

            EditorGUI.EndDisabledGroup();
        }

        public void DrawSlider(Rect position)
        {
            position.height = GetYSize(GUI.skin.button);

            IPlayable? playable = target;
            double length = playable?.length ?? 0;
            bool isTimelineValid = playable != null && double.IsFinite(length) && length > 0;

            float sliderLength = isTimelineValid ? length.ClampToFloat() : 1;
            float sliderTime = 0;
            if (isTimelineValid)
            {
                double time = playable!.time;
                if (double.IsFinite(time))
                    sliderTime = time.Clamp(0, length).ClampToFloat();
            }

            ILoopablePlayer? loopablePlayer = playable as ILoopablePlayer;
            Rect timelineTrackPosition = GetTimelineTrackPosition(position);
            int loopRangeControlID = GUIUtility.GetControlID(loopRangeHash, FocusType.Passive, position);
            bool loopHandlesEnabled = isTimelineValid && loopablePlayer != null;

            if (loopHandlesEnabled)
                HandleLoopRangeInput(loopRangeControlID, position, timelineTrackPosition, loopablePlayer!, length);
            else if (GUIUtility.hotControl == loopRangeControlID)
            {
                GUIUtility.hotControl = 0;
                draggedLoopHandle = LoopHandle.none;
            }

            EditorGUI.BeginDisabledGroup(!isTimelineValid || playable == null || !playable.isPlaying);

            EditorGUI.BeginChangeCheck();
            float sliderValue = GUI.HorizontalSlider(position, sliderTime, 0, sliderLength);
            if (EditorGUI.EndChangeCheck() && playable != null)
                playable.time = sliderValue;

            if (loopHandlesEnabled)
                DrawLoopHandles(loopRangeControlID, position, timelineTrackPosition, loopablePlayer!, length);

            position.y += position.height;
            position.y += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < timeUnits.Count; i++)
            {
                ITimeUnit timeUnit = timeUnits[i];
                if (timeUnitContents.Count <= i)
                    timeUnitContents.Add(new TimeUnitContents());

                TimeUnitContents contents = timeUnitContents[i];
                if (Event.current.type == EventType.Repaint || !contents.initialized)
                {
                    contents.time.text = RichNumberMSpace(timeUnit.TimeToString(playable));
                    contents.remainingTime.text = RichNumberMSpace(timeUnit.RemainingTimeToString(playable));
                    contents.length.text = RichNumberMSpace(timeUnit.LengthToString(playable));
                    contents.initialized = true;
                }

                position.height = timeUnit.GetHeight();

                BeginFontSize(11, RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleLeft, RuniStyles.richLabel);
                GUI.Label(position, contents.time, RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleCenter, RuniStyles.richLabel);
                GUI.Label(position, contents.remainingTime, RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleRight, RuniStyles.richLabel);
                GUI.Label(position, contents.length, RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                EndFontSize(RuniStyles.richLabel);

                position.y += position.height;
                position.y += EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.EndDisabledGroup();
        }

        void HandleLoopRangeInput
        (
            int controlID,
            Rect timelinePosition,
            Rect trackPosition,
            ILoopablePlayer loopablePlayer,
            double length
        )
        {
            GetLoopHandlePositions(timelinePosition, trackPosition, loopablePlayer, length, out Rect startHandlePosition, out Rect endHandlePosition);

            Event currentEvent = Event.current;
            switch (currentEvent.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (currentEvent.button != 0)
                        break;

                    if (startHandlePosition.Contains(currentEvent.mousePosition))
                        draggedLoopHandle = LoopHandle.start;
                    else if (endHandlePosition.Contains(currentEvent.mousePosition))
                        draggedLoopHandle = LoopHandle.end;
                    else
                        break;

                    GUIUtility.hotControl = controlID;
                    currentEvent.Use();
                    break;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != controlID || draggedLoopHandle == LoopHandle.none)
                        break;

                    double time = PositionToTime(trackPosition, currentEvent.mousePosition.x, length);
                    if (draggedLoopHandle == LoopHandle.start)
                    {
                        double loopEnd = double.IsNaN(loopablePlayer.loopEnd) ? time : Math.Max(loopablePlayer.loopEnd, time);
                        SetLoopRange(loopablePlayer, time, loopEnd);
                    }
                    else
                    {
                        double loopStart = double.IsNaN(loopablePlayer.loopStart) ? time : Math.Min(loopablePlayer.loopStart, time);
                        SetLoopRange(loopablePlayer, loopStart, time);
                    }

                    GUI.changed = true;
                    currentEvent.Use();
                    break;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl != controlID)
                        break;

                    GUIUtility.hotControl = 0;
                    draggedLoopHandle = LoopHandle.none;
                    currentEvent.Use();
                    break;
            }
        }

        void DrawLoopHandles
        (
            int controlID,
            Rect timelinePosition,
            Rect trackPosition,
            ILoopablePlayer loopablePlayer,
            double length
        )
        {
            double loopStart = GetTimelineValue(loopablePlayer.loopStart, 0, length);
            double loopEnd = GetTimelineValue(loopablePlayer.loopEnd, length, length);

            float startX = TimeToPosition(trackPosition, loopStart, length);
            float endX = TimeToPosition(trackPosition, loopEnd, length);

            Color handleColor = new Color(0.2f, 0.65f, 1, loopablePlayer.loop ? 1 : 0.55f);
            EditorGUI.DrawRect(new Rect(startX - 1, timelinePosition.y + 4, 2, timelinePosition.height - 8), handleColor);
            EditorGUI.DrawRect(new Rect(endX - 1, timelinePosition.y + 4, 2, timelinePosition.height - 8), handleColor);

            Handles.BeginGUI();
            Color oldColor = Handles.color;

            Handles.color = GUIUtility.hotControl == controlID && draggedLoopHandle == LoopHandle.start ? Color.white : handleColor;
            startHandleVertices[0] = new Vector3(startX - 5, timelinePosition.y);
            startHandleVertices[1] = new Vector3(startX + 5, timelinePosition.y);
            startHandleVertices[2] = new Vector3(startX, timelinePosition.y + 7);
            Handles.DrawAAConvexPolygon(startHandleVertices);

            Handles.color = GUIUtility.hotControl == controlID && draggedLoopHandle == LoopHandle.end ? Color.white : handleColor;
            endHandleVertices[0] = new Vector3(endX - 5, timelinePosition.yMax);
            endHandleVertices[1] = new Vector3(endX + 5, timelinePosition.yMax);
            endHandleVertices[2] = new Vector3(endX, timelinePosition.yMax - 7);
            Handles.DrawAAConvexPolygon(endHandleVertices);

            Handles.color = oldColor;
            Handles.EndGUI();

            GetLoopHandlePositions(timelinePosition, trackPosition, loopablePlayer, length, out Rect startHandlePosition, out Rect endHandlePosition);
            GUI.Label(startHandlePosition, TempContent(string.Empty, GetTextOrKey("runios-editor:inspector.runi_audio_source.transport.loop_start_handle.tooltip")), GUIStyle.none);
            GUI.Label(endHandlePosition, TempContent(string.Empty, GetTextOrKey("runios-editor:inspector.runi_audio_source.transport.loop_end_handle.tooltip")), GUIStyle.none);
        }

        static void GetLoopHandlePositions
        (
            Rect timelinePosition,
            Rect trackPosition,
            ILoopablePlayer loopablePlayer,
            double length,
            out Rect startHandlePosition,
            out Rect endHandlePosition
        )
        {
            double loopStart = GetTimelineValue(loopablePlayer.loopStart, 0, length);
            double loopEnd = GetTimelineValue(loopablePlayer.loopEnd, length, length);

            float startX = TimeToPosition(trackPosition, loopStart, length);
            float endX = TimeToPosition(trackPosition, loopEnd, length);
            const float handleHeight = 7;

            startHandlePosition = new Rect(startX - 7, timelinePosition.y, 14, handleHeight);
            endHandlePosition = new Rect(endX - 7, timelinePosition.yMax - handleHeight, 14, handleHeight);
        }

        static Rect GetTimelineTrackPosition(Rect timelinePosition)
        {
            float thumbWidth = Mathf.Max(GUI.skin.horizontalSliderThumb.fixedWidth, 10);
            timelinePosition.xMin += thumbWidth * 0.5f;
            timelinePosition.xMax -= thumbWidth * 0.5f;
            return timelinePosition;
        }

        static double GetTimelineValue(double value, double defaultValue, double length)
        {
            if (double.IsNaN(value))
                return defaultValue;

            if (double.IsPositiveInfinity(value))
                return length;

            if (double.IsNegativeInfinity(value))
                return 0;

            return value.Clamp(0, length);
        }

        static float TimeToPosition(Rect trackPosition, double time, double length) => trackPosition.xMin.Lerp(trackPosition.xMax, (time / length).ClampToFloat());

        static double PositionToTime(Rect trackPosition, float position, double length)
        {
            if (trackPosition.width <= 0)
                return 0;

            return (((position - trackPosition.xMin) / trackPosition.width).Clamp01()) * length;
        }

        void SetLoopRange(ILoopablePlayer loopablePlayer, double loopStart, double loopEnd)
        {
            if (loopRangeSetter != null)
                loopRangeSetter.Invoke(loopablePlayer, loopStart, loopEnd);
            else
            {
                loopablePlayer.loopStart = loopStart;
                loopablePlayer.loopEnd = loopEnd;
            }
        }

        public float GetHeight()
        {
            float height = GetYSize(GUI.skin.button);
            height += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < timeUnits.Count; i++)
            {
                height += timeUnits[i].GetHeight();
                height += EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
