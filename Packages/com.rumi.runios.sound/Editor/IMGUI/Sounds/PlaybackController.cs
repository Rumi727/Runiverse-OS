#nullable enable
using RuniOS.Linq;
using RuniOS.Sounds;
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Editor.IMGUI.Sounds
{
    /// <summary>
    /// Draws playback controls for one or more <see cref="IAudioPlayer"/> instances.<br/>
    /// 하나 이상의 <see cref="IAudioPlayer"/> 인스턴스를 위한 재생 제어 UI를 그립니다.
    /// </summary>
    /// <remarks>
    /// The available controls are selected through <see cref="features"/>, and the target interfaces determine which operations are applied.<br/>
    /// <see cref="features"/>를 통해 표시할 컨트롤을 선택하며, 대상이 구현한 인터페이스에 따라 적용할 작업이 결정됩니다.
    /// </remarks>
    public sealed class PlaybackController
    {
        enum LoopHandle
        {
            none,
            start,
            end
        }

        static readonly int loopRangeHash = "PlayableControllerLoopRange".GetHashCode();
        const double skipSeconds = 1;

        /// <summary>
        /// Initializes an empty playback controller without additional time units.<br/>
        /// 추가 시간 단위 없이 비어 있는 재생 컨트롤러를 초기화합니다.
        /// </summary>
        public PlaybackController() => timeUnits = [];

        /// <summary>
        /// Initializes a playback controller with the specified time units.<br/>
        /// 지정된 시간 단위로 재생 컨트롤러를 초기화합니다.
        /// </summary>
        /// <param name="timeUnits">
        /// The time units to display with the controller.<br/>
        /// 컨트롤러와 함께 표시할 시간 단위입니다.
        /// </param>
        public PlaybackController(params ITimeUnit[] timeUnits) => this.timeUnits = timeUnits.ToList();

        /// <summary>
        /// Initializes a playback controller with the time units copied from the specified collection.<br/>
        /// 지정된 컬렉션에서 시간 단위를 복사하여 재생 컨트롤러를 초기화합니다.
        /// </summary>
        /// <param name="timeUnits">
        /// The collection of time units to copy and display with the controller.<br/>
        /// 복사하여 컨트롤러와 함께 표시할 시간 단위 컬렉션입니다.
        /// </param>
        public PlaybackController(IEnumerable<ITimeUnit> timeUnits) => this.timeUnits = timeUnits.ToList();

        /// <summary>
        /// Gets the mutable collection of audio players controlled by this instance.<br/>
        /// 이 인스턴스가 제어하는 오디오 플레이어의 변경 가능한 컬렉션을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Mixed target types are supported. Each control filters <see cref="targets"/> by the interface required for that operation.<br/>
        /// <list type="bullet">
        /// <item><description>
        /// The play button invokes <see cref="IPlayControl.Play"/> on every <see cref="IPlayControl"/> target. The stop button invokes <see cref="IStoppable.Stop"/> on every <see cref="IStoppable"/> target and is enabled when at least one such target is playing.
        /// </description></item>
        /// <item><description>
        /// When <see cref="PlaybackControllerFeatures.pause"/> is enabled, pause controls operate on every <see cref="IPausable"/> target. The unpause action is used when all such targets are paused; otherwise, the pause action is used.
        /// </description></item>
        /// <item><description>
        /// When <see cref="PlaybackControllerFeatures.skip"/> is enabled, skip changes the time of every <see cref="ISeekable"/> target. It is enabled only when at least one target is playing and at least one seekable target exists.
        /// </description></item>
        /// <item><description>
        /// The timeline slider, timeline labels, and <see cref="timelineOverlay"/> use the result of <see cref="GetTarget{T}"/> with <see cref="ISeekable"/>. Zero or multiple seekable targets produce no unique timeline target, so the slider is disabled.
        /// </description></item>
        /// <item><description>
        /// Loop handles additionally require <see cref="PlaybackControllerFeatures.loopRange"/> and one unique seekable target that also implements <see cref="ILoopControl"/>.
        /// </description></item>
        /// <item><description>
        /// Time-unit fields receive the complete <see cref="targets"/> collection, allowing each <see cref="ITimeUnit"/> implementation to handle mixed types. Timeline labels receive the unique seekable target or <see langword="null"/>.
        /// </description></item>
        /// </list>
        /// <br/><br/>
        /// 서로 다른 타입의 대상을 지원합니다. 각 컨트롤은 작업에 필요한 인터페이스를 기준으로 <see cref="targets"/>를 필터링합니다.
        /// <list type="bullet">
        /// <item><description>
        /// 재생 버튼은 모든 <see cref="IPlayControl"/> 대상에 <see cref="IPlayControl.Play"/>를 호출합니다. 정지 버튼은 모든 <see cref="IStoppable"/> 대상에 <see cref="IStoppable.Stop"/>을 호출하며, 해당 대상 중 하나라도 재생 중이면 활성화됩니다.
        /// </description></item>
        /// <item><description>
        /// <see cref="PlaybackControllerFeatures.pause"/>가 활성화되면 일시 정지 컨트롤은 모든 <see cref="IPausable"/> 대상에 적용됩니다. 해당 대상이 모두 일시 정지 상태이면 재생 재개 동작을 사용하고, 그렇지 않으면 일시 정지 동작을 사용합니다.
        /// </description></item>
        /// <item><description>
        /// <see cref="PlaybackControllerFeatures.skip"/>가 활성화되면 스킵은 모든 <see cref="ISeekable"/> 대상의 시간을 변경합니다. 하나 이상의 대상이 재생 중이고 탐색 가능한 대상이 하나 이상 있을 때만 활성화됩니다.
        /// </description></item>
        /// <item><description>
        /// 타임라인 슬라이더, 타임라인 라벨 및 <see cref="timelineOverlay"/>는 <see cref="ISeekable"/>을 대상으로 한 <see cref="GetTarget{T}"/>의 결과를 사용합니다. 탐색 가능한 대상이 없거나 둘 이상이면 고유한 타임라인 대상이 없어 슬라이더가 비활성화됩니다.
        /// </description></item>
        /// <item><description>
        /// 반복 핸들은 <see cref="PlaybackControllerFeatures.loopRange"/>가 활성화되고, 탐색 가능한 고유 대상 하나가 <see cref="ILoopControl"/>도 구현할 때만 사용할 수 있습니다.
        /// </description></item>
        /// <item><description>
        /// 시간 단위 필드에는 전체 <see cref="targets"/> 컬렉션이 전달되므로 각 <see cref="ITimeUnit"/> 구현이 서로 다른 타입을 처리할 수 있습니다. 타임라인 라벨에는 고유한 탐색 가능 대상 또는 <see langword="null"/>이 전달됩니다.
        /// </description></item>
        /// </list>
        /// </remarks>
        public List<IAudioPlayer> targets { get; } = [];

        /// <summary>
        /// Gets the only target when exactly one target is registered; otherwise, <see langword="null"/>.<br/>
        /// 대상이 정확히 하나 등록된 경우 해당 대상을 가져오고, 그렇지 않으면 <see langword="null"/>을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Setting this property clears <see cref="targets"/> before adding the assigned target.<br/>
        /// 이 속성을 설정하면 할당된 대상을 추가하기 전에 <see cref="targets"/>를 비웁니다.
        /// </remarks>
        [DisallowNull]
        public IAudioPlayer? target
        {
            get => targets.Count == 1 ? targets[0] : null;
            set
            {
                targets.Clear();
                targets.Add(value);
            }
        }

        /// <summary>
        /// Gets or sets the feature flags used when drawing this controller.<br/>
        /// 이 컨트롤러를 그릴 때 사용할 기능 플래그를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// The default value is <see cref="PlaybackControllerFeatures.all"/>.<br/>
        /// 기본값은 <see cref="PlaybackControllerFeatures.all"/>입니다.
        /// </remarks>
        public PlaybackControllerFeatures features { get; set; } = PlaybackControllerFeatures.all;

        /// <summary>
        /// Gets the mutable collection of time units displayed by the controller.<br/>
        /// 컨트롤러가 표시하는 시간 단위의 변경 가능한 컬렉션을 가져옵니다.
        /// </summary>
        public List<ITimeUnit> timeUnits { get; }

        /// <summary>
        /// Sets the callback used to draw an overlay on the timeline track.<br/>
        /// 타임라인 트랙에 오버레이를 그리는 데 사용할 콜백을 설정합니다.
        /// </summary>
        /// <remarks>
        /// The callback receives the track position, the unique seekable target, and its length when such a target exists.<br/>
        /// 콜백은 고유한 탐색 가능 대상이 존재할 때 트랙 위치, 해당 대상 및 길이를 전달받습니다.
        /// <br/><br/>
        /// Assign <see langword="null"/> to disable the overlay callback.<br/>
        /// <see langword="null"/>을 할당하면 오버레이 콜백을 비활성화합니다.
        /// </remarks>
        public Action<Rect, ISeekable, double>? timelineOverlay { private get; set; }

        LoopHandle draggedLoopHandle;
        readonly Vector3[] startHandleVertices = new Vector3[3];
        readonly Vector3[] endHandleVertices = new Vector3[3];

        /// <summary>
        /// Gets the unique target assignable to <typeparamref name="T"/>.<br/>
        /// <typeparamref name="T"/>에 할당할 수 있는 고유한 대상을 가져옵니다.
        /// </summary>
        /// <typeparam name="T">
        /// The target type to search for. It must implement <see cref="IAudioPlayer"/>.<br/>
        /// 검색할 대상 타입입니다. <see cref="IAudioPlayer"/>를 구현해야 합니다.
        /// </typeparam>
        /// <returns>
        /// The matching target when exactly one target is assignable to <typeparamref name="T"/>; otherwise, <see langword="null"/>.<br/>
        /// <typeparamref name="T"/>에 할당할 수 있는 대상이 정확히 하나이면 해당 대상을 반환하고, 그렇지 않으면 <see langword="null"/>을 반환합니다.
        /// </returns>
        public T? GetTarget<T>() where T : IAudioPlayer
        {
            T? result = default;
            int count = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] is not T item)
                    continue;

                result = item;
                count++;

                if (count > 1)
                    return default;
            }

            return result;
        }

        /// <summary>
        /// Draws the controller using a layout-managed control rectangle.<br/>
        /// 레이아웃으로 관리되는 컨트롤 사각형을 사용하여 컨트롤러를 그립니다.
        /// </summary>
        public void DrawLayout() => Draw(EditorGUILayout.GetControlRect(false, GetHeight()));

        /// <summary>
        /// Draws the enabled controller controls within the specified position.<br/>
        /// 지정된 위치 안에 활성화된 컨트롤러를 그립니다.
        /// </summary>
        /// <param name="position">
        /// The GUI area in which to draw the controller.<br/>
        /// 컨트롤러를 그릴 GUI 영역입니다.
        /// </param>
        public void Draw(Rect position)
        {
            float orgWidth = position.width;
            if (features.HasFlag(PlaybackControllerFeatures.timeline))
                position.width = EditorGUIUtility.labelWidth;

            DrawControl(position);

            if (features.HasFlag(PlaybackControllerFeatures.timeline))
            {
                position.x += position.width + 8;
                position.width = orgWidth - (position.width + 8);

                DrawSlider(position);
            }
        }

        /// <summary>
        /// Draws playback buttons and, when the timeline feature is enabled, the configured time-unit fields.<br/>
        /// 재생 버튼과 타임라인 기능이 활성화된 경우 구성된 시간 단위 필드를 그립니다.
        /// </summary>
        /// <param name="position">
        /// The GUI area in which to draw the control buttons and time-unit fields.<br/>
        /// 컨트롤 버튼과 시간 단위 필드를 그릴 GUI 영역입니다.
        /// </param>
        public void DrawControl(Rect position)
        {
            GUIStyle buttonLeft = GUI.skin.FindStyle("buttonleft");
            GUIStyle buttonMid = GUI.skin.FindStyle("buttonmid");
            GUIStyle buttonRight = GUI.skin.FindStyle("buttonright");

            float orgX = position.x;
            float orgWidth = position.width;

            position.height = GetYSize(GUI.skin.button);

            bool anyPlaying = targets.Any(t => t.isPlaying);

            int controlButtonCount = features.HasFlag(PlaybackControllerFeatures.pause) ? 3 : 2;
            if (features.HasFlag(PlaybackControllerFeatures.skip))
                position.width = (orgWidth * (2f / 3f)) / controlButtonCount;
            else
                position.width = orgWidth / controlButtonCount;

            {
                EditorGUI.BeginDisabledGroup(targets.OfType<IPlayControl>().IsEmpty());

                bool click = false;
                if (anyPlaying)
                {
                    if (GUI.Button(position, "▶↻", buttonLeft))
                        click = true;
                }
                else if (GUI.Button(position, "▶", buttonLeft))
                    click = true;

                if (click)
                {
                    foreach (var target in targets.OfType<IPlayControl>())
                        target.Play();
                }

                EditorGUI.EndDisabledGroup();
            }

            if (features.HasFlag(PlaybackControllerFeatures.pause))
            {
                EditorGUI.BeginDisabledGroup(targets.OfType<IPausable>().IsEmpty());

                position.x += position.width;

                bool allPaused = targets.OfType<IPausable>().All(t => t.isPaused);
                if (allPaused)
                {
                    if (GUI.Button(position, "▶▮", buttonMid))
                    {
                        foreach (var target in targets.OfType<IPausable>())
                            target.UnPause();
                    }
                }
                else if (GUI.Button(position, "▮▮", buttonMid))
                {
                    foreach (var target in targets.OfType<IPausable>())
                        target.Pause();
                }

                EditorGUI.EndDisabledGroup();
            }

            {
                EditorGUI.BeginDisabledGroup(targets.OfType<IStoppable>().Where(x => x.isPlaying).IsEmpty());

                position.x += position.width;
                position.width = position.width.Floor();

                if (GUI.Button(position, "■", buttonRight))
                {
                    foreach (var target in targets.OfType<IStoppable>())
                        target.Stop();
                }

                EditorGUI.EndDisabledGroup();
            }

            if (features.HasFlag(PlaybackControllerFeatures.skip))
            {
                position.x += position.width + 2;

                float allButtonWidth = (orgWidth * (1f / 3f)) - 2;
                float buttonWidth = (allButtonWidth / 2f).Round();

                position.width = buttonWidth;

                EditorGUI.BeginDisabledGroup(!anyPlaying || targets.OfType<ISeekable>().IsEmpty());

                BeginFontSize(10, buttonLeft);
                BeginFontSize(10, buttonRight);

                if (GUI.Button(position, "◀◀", buttonLeft))
                    Skip(-skipSeconds);

                position.x += buttonWidth;
                position.width = (orgWidth - (position.x - orgX)).Round();

                if (GUI.Button(position, "▶▶", buttonRight))
                    Skip(skipSeconds);

                EndFontSize(buttonRight);
                EndFontSize(buttonLeft);

                EditorGUI.EndDisabledGroup();
            }

            if (features.HasFlag(PlaybackControllerFeatures.timeline))
            {
                position.x = orgX;
                position.width = orgWidth;

                position.y += position.height;
                position.y += EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.BeginDisabledGroup(!anyPlaying);

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
        }

        void Skip(double seconds)
        {
            foreach (var playable in targets.OfType<ISeekable>())
                playable.time += seconds;
        }

        /// <summary>
        /// Draws the timeline slider, loop handles, overlay, and time labels.<br/>
        /// 타임라인 슬라이더, 반복 핸들, 오버레이 및 시간 라벨을 그립니다.
        /// </summary>
        /// <param name="position">
        /// The GUI area in which to draw the timeline.<br/>
        /// 타임라인을 그릴 GUI 영역입니다.
        /// </param>
        public void DrawSlider(Rect position)
        {
            position.height = GetYSize(GUI.skin.button);

            ISeekable? timeControllable = GetTarget<ISeekable>();
            double time = timeControllable?.time ?? 0;
            double length = timeControllable?.length ?? 0;

            ILoopControl? loopablePlayer = timeControllable as ILoopControl;
            bool loopHandlesEnabled = features.HasFlag(PlaybackControllerFeatures.loopRange) && loopablePlayer != null;

            Rect timelineTrackPosition = GetTimelineTrackPosition(position);
            int loopRangeControlID = GUIUtility.GetControlID(loopRangeHash, FocusType.Passive, position);

            if (loopHandlesEnabled)
                HandleLoopRangeInput(loopRangeControlID, position, timelineTrackPosition, loopablePlayer!, length);
            else if (GUIUtility.hotControl == loopRangeControlID)
            {
                GUIUtility.hotControl = 0;
                draggedLoopHandle = LoopHandle.none;
            }

            EditorGUI.BeginDisabledGroup(timeControllable == null || !timeControllable.isPlaying);

            EditorGUI.BeginChangeCheck();
            float sliderValue = GUI.HorizontalSlider(position, time.ClampToFloat(), 0, length.ClampToFloat());
            if (EditorGUI.EndChangeCheck() && timeControllable != null)
                timeControllable.time = sliderValue;

            if (timeControllable != null)
                timelineOverlay?.Invoke(timelineTrackPosition, timeControllable, length);

            if (loopHandlesEnabled)
                DrawLoopHandles(loopRangeControlID, position, timelineTrackPosition, loopablePlayer!, length);

            position.y += position.height;
            position.y += EditorGUIUtility.standardVerticalSpacing;

            for (int i = 0; i < timeUnits.Count; i++)
            {
                ITimeUnit timeUnit = timeUnits[i];
                position.height = timeUnit.GetHeight();

                BeginFontSize(11, RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleLeft, RuniStyles.richLabel);
                GUI.Label(position, TempContent(RichNumberMSpace(timeUnit.TimeToString(timeControllable))), RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleCenter, RuniStyles.richLabel);
                GUI.Label(position, TempContent(RichNumberMSpace(timeUnit.RemainingTimeToString(timeControllable))), RuniStyles.richLabel);
                EndAlignment(RuniStyles.richLabel);

                BeginAlignment(TextAnchor.MiddleRight, RuniStyles.richLabel);
                GUI.Label(position, TempContent(RichNumberMSpace(timeUnit.LengthToString(timeControllable))), RuniStyles.richLabel);
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
            ILoopControl loopControl,
            double length
        )
        {
            GetLoopHandlePositions(timelinePosition, trackPosition, loopControl, length, out Rect startHandlePosition, out Rect endHandlePosition);

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
                        loopControl.loopStart = time;
                    else
                        loopControl.loopEnd = time;

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
            ILoopControl loopControl,
            double length
        )
        {
            double loopStart = GetTimelineValue(loopControl.loopStart, 0, length);
            double loopEnd = GetTimelineValue(loopControl.loopEnd, length, length);

            float startX = TimeToPosition(trackPosition, loopStart, length);
            float endX = TimeToPosition(trackPosition, loopEnd, length);

            Color handleColor = new Color(0.2f, 0.65f, 1, loopControl.loop ? 1 : 0.55f);
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

            GetLoopHandlePositions(timelinePosition, trackPosition, loopControl, length, out Rect startHandlePosition, out Rect endHandlePosition);
            GUI.Label(startHandlePosition, TempContent(string.Empty, GetTextOrKey("runios-editor:inspector.runi_audio_source.transport.loop_start_handle.tooltip")), GUIStyle.none);
            GUI.Label(endHandlePosition, TempContent(string.Empty, GetTextOrKey("runios-editor:inspector.runi_audio_source.transport.loop_end_handle.tooltip")), GUIStyle.none);
        }

        static void GetLoopHandlePositions
        (
            Rect timelinePosition,
            Rect trackPosition,
            ILoopControl loopControl,
            double length,
            out Rect startHandlePosition,
            out Rect endHandlePosition
        )
        {
            double loopStart = GetTimelineValue(loopControl.loopStart, 0, length);
            double loopEnd = GetTimelineValue(loopControl.loopEnd, length, length);

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

        /// <summary>
        /// Gets the height required to draw the controller with its current features and time units.<br/>
        /// 현재 기능과 시간 단위로 컨트롤러를 그리는 데 필요한 높이를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The required GUI height in pixels.<br/>
        /// 필요한 GUI 높이(픽셀)입니다.
        /// </returns>
        public float GetHeight()
        {
            float height = GetYSize(GUI.skin.button);
            if (!features.HasFlag(PlaybackControllerFeatures.timeline))
                return height;

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
