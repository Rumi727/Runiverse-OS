#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI;
using RuniOS.NBS;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    /// <summary>
    /// Draws NBS resource, scheduling, transport, visual-effect preview, and timing information for <see cref="NBSPlayer"/>.<br/>
    /// <see cref="NBSPlayer"/>의 NBS 리소스, 예약, 트랜스포트, 시각 효과 미리보기 및 타이밍 정보를 그립니다.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NBSPlayer), true)]
    public sealed class NBSPlayerEditor : RuniAudioSourceEditor<NBSPlayer>
    {
        const float visualEffectCardHeight = 44;
        const double savePopupHoldDuration = 1;
        const double savePopupFadeDuration = 0.25;
        static readonly Color defaultAccentColor = new Color32(0, 120, 212, byte.MaxValue);

        readonly Dictionary<EntityId, SavePopupTracker> savePopupTrackers = [];
        double savePopupStartedAt = double.NegativeInfinity;
        GUIStyle? visualEffectStatusStyle;
        GUIStyle? savePopupStyle;

        public NBSPlayerEditor()
        {
            playableController.timeUnits.Add(new NBSTickTimeUnit());
            playableController.timeUnits.Add(new NBSIndexTimeUnit());
        }

        protected override bool repaintInEditor
        {
            get
            {
                if (base.repaintInEditor || EditorApplication.timeSinceStartup < savePopupStartedAt + savePopupHoldDuration + savePopupFadeDuration)
                    return true;

                if (targets.IsDefaultOrEmpty)
                    return false;

                foreach (NBSPlayer? player in targets)
                {
                    if (player != null && player.nbsFile != null && player.nbsFile.visualEffectMap.GetState(player.tick).rainbowEnabled)
                        return true;
                }

                return false;
            }
        }

        protected override void OnEnable()
        {
            ResetSavePopupTracking();
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            ResetSavePopupTracking();
            base.OnDisable();
        }

        protected override bool DrawSourceLayout()
        {
            GUILayout.Label(TrTempContent("runios-editor:inspector.nbs_player.source.header"), EditorStyles.boldLabel);

            if (EditPropertyValue
            (
                "_nbsFileRef",
                x => x.nbsFileRef,
                (position, x) => RuniFields.AssetRefField
                (
                    position,
                    TrTempContent("runios-editor:inspector.nbs_player.source.file"),
                    x.nbsFileRef
                ),
                (x, value) => x.nbsFileRef = value,
                x => RuniFields.GetAssetRefFieldHeight
                (
                    TrTempContent("runios-editor:inspector.nbs_player.source.file"),
                    x.nbsFileRef
                )
            ))
                ForEach(x => ReloadAndRepaint(x).Forget());

            EditPropertyValue
            (
                "_nonRigidbodyVelocity",
                x => x.nonRigidbodyVelocity,
                (position, x) => EditorGUI.Toggle
                (
                    position,
                    TrTempContent
                    (
                        "runios-editor:inspector.wave_audio_source.source.non_rigidbody_velocity",
                        "runios-editor:inspector.wave_audio_source.source.non_rigidbody_velocity.tooltip"
                    ),
                    x.nonRigidbodyVelocity
                ),
                (x, value) => x.nonRigidbodyVelocity = value
            );

            EditPropertyValue
            (
                "_useFileLoopSettings",
                x => x.useFileLoopSettings,
                (position, x) => EditorGUI.Toggle
                (
                    position,
                    TrTempContent("runios-editor:inspector.nbs_player.source.use_file_loop_settings"),
                    x.useFileLoopSettings
                ),
                (x, value) => x.useFileLoopSettings = value
            );
            return true;
        }

        protected override void DrawTransportLayout()
        {
            EditorGUI.BeginChangeCheck();
            base.DrawTransportLayout();
            if (EditorGUI.EndChangeCheck())
                ResetSavePopupTracking();

            Space();

            EditorGUI.BeginChangeCheck();
            double value = EditorGUILayout.DoubleField
            (
                TrTempContent
                (
                    "runios-editor:inspector.nbs_player.transport.worker_interval",
                    "runios-editor:inspector.nbs_player.transport.worker_interval.tooltip"
                ),
                NBSPlaybackSettings.workerInterval
            );
            if (EditorGUI.EndChangeCheck() && double.IsFinite(value) && value > 0)
                NBSPlaybackSettings.workerInterval = value;

            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.DoubleField
            (
                TrTempContent
                (
                    "runios-editor:inspector.nbs_player.transport.scheduling_lookahead",
                    "runios-editor:inspector.nbs_player.transport.scheduling_lookahead.tooltip"
                ),
                NBSPlaybackSettings.schedulingLookahead
            );
            if (EditorGUI.EndChangeCheck() && double.IsFinite(value) && value >= 0)
                NBSPlaybackSettings.schedulingLookahead = value;

            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.DoubleField
            (
                TrTempContent
                (
                    "runios-editor:inspector.nbs_player.transport.late_tolerance",
                    "runios-editor:inspector.nbs_player.transport.late_tolerance.tooltip"
                ),
                NBSPlaybackSettings.lateTolerance
            );
            if (EditorGUI.EndChangeCheck() && double.IsFinite(value) && value >= 0)
                NBSPlaybackSettings.lateTolerance = value;
        }

        protected override void DrawInformationLayout()
        {
            if (DrawVisualEffectsLayout())
                Space();

            base.DrawInformationLayout();
        }

        protected override void DrawAdditionalSpatialLayout()
        {
            EditPropertyValue
            (
                "_rolloffMode",
                x => x.rolloffMode,
                (position, x) => (SoundRolloffMode)EditorGUI.EnumPopup
                (
                    position,
                    TrTempContent("runios-editor:inspector.wave_audio_source.spatial.rolloff_mode"),
                    x.rolloffMode
                ),
                (x, value) => x.rolloffMode = value
            );
        }

        protected override void DrawAdditionalInformationLayout()
        {
            EditorGUILayout.LabelField
            (
                TrTempContent("runios-editor:inspector.nbs_player.information.tick_length"),
                new GUIContent(GetCommonValueString(x => x.tickLength))
            );
            EditorGUILayout.LabelField
            (
                TrTempContent("runios-editor:inspector.nbs_player.information.index_length"),
                new GUIContent(GetCommonValueString(x => x.indexLength))
            );
            EditorGUILayout.LabelField
            (
                TrTempContent("runios-editor:inspector.nbs_player.information.tps"),
                new GUIContent(GetCommonValueString(x => x.ticksPerSecond))
            );
            EditorGUILayout.LabelField
            (
                TrTempContent("runios-editor:inspector.nbs_player.information.bpm"),
                new GUIContent(GetCommonValueString(x => x.beatsPerMinute))
            );
        }

        bool DrawVisualEffectsLayout()
        {
            bool hasVisualEffects = false;
            bool hasFirstState = false;
            bool hasMixedState = false;
            NBSVisualEffectMap.State? commonState = null;

            foreach (NBSPlayer? player in targets)
            {
                if (player == null)
                    continue;

                NBSFile? file = player.nbsFile;
                UpdateSavePopupTracking(player, file);

                if (file?.visualEffectMap.hasEvents == true)
                    hasVisualEffects = true;

                NBSVisualEffectMap.State? state = file?.visualEffectMap.GetState(player.tick);
                if (!hasFirstState)
                {
                    commonState = state;
                    hasFirstState = true;
                }
                else if (commonState != state)
                    hasMixedState = true;
            }

            if (!hasVisualEffects)
                return false;

            GUILayout.Label(TrTempContent("runios-editor:inspector.nbs_player.visual_effects.header"), EditorStyles.boldLabel);

            NBSVisualEffectMap.State stateToDraw = commonState ?? default;
            Color accentColor = GetAccentColor(stateToDraw);
            Rect cardPosition = EditorGUILayout.GetControlRect(false, visualEffectCardHeight);
            GUI.Box(cardPosition, GUIContent.none, EditorStyles.helpBox);

            Rect innerPosition = new Rect(cardPosition.x + 1, cardPosition.y + 1, cardPosition.width - 2, cardPosition.height - 2);
            if (!hasMixedState && stateToDraw.backgroundAccentEnabled)
            {
                Color backgroundColor = accentColor;
                backgroundColor.a = 0.15f;
                EditorGUI.DrawRect(innerPosition, backgroundColor);
            }

            Rect accentPosition = new Rect(cardPosition.x + 1, cardPosition.y + 1, 4, cardPosition.height - 2);
            EditorGUI.DrawRect(accentPosition, hasMixedState ? Color.gray : accentColor);

            string status = hasMixedState
                ? GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.mixed")
                : GetVisualEffectStatus(stateToDraw);
            GUIStyle statusStyle = GetVisualEffectStatusStyle();
            GUIContent statusContent = TempContent(status);
            Vector2 statusSize = statusStyle.CalcSize(statusContent);
            float statusHeight = Mathf.Min(statusSize.y, Mathf.Max(0, cardPosition.height - 2));
            Rect statusPosition = new Rect
            (
                cardPosition.x + 10,
                (cardPosition.y - (statusHeight * 0.5f)) + 10,
                Mathf.Max(0, cardPosition.width - 16),
                statusHeight
            );
            GUI.Label(statusPosition, statusContent, statusStyle);

            DrawSavePopup(cardPosition);
            return true;
        }

        void UpdateSavePopupTracking(NBSPlayer player, NBSFile? file)
        {
            EntityId instanceId = player.GetEntityId();
            double currentTick = file == null ? 0 : player.tick;
            bool isContinuouslyPlaying = player.isPlaying && !player.isPaused;

            if (!savePopupTrackers.TryGetValue(instanceId, out SavePopupTracker? tracker))
            {
                savePopupTrackers.Add(instanceId, new SavePopupTracker(file, currentTick, isContinuouslyPlaying));
                return;
            }

            if (ReferenceEquals(tracker.file, file) && tracker.wasPlaying && isContinuouslyPlaying && file != null &&
                TryGetSavePopupCrossing(player, file, tracker.tick, currentTick))
                savePopupStartedAt = EditorApplication.timeSinceStartup;

            tracker.file = file;
            tracker.tick = currentTick;
            tracker.wasPlaying = isContinuouslyPlaying;
        }

        static bool TryGetSavePopupCrossing(NBSPlayer player, NBSFile file, double previousTick, double currentTick)
        {
            NBSVisualEffectMap map = file.visualEffectMap;
            if (map.savePopupTicks.Count == 0 || !double.IsFinite(previousTick) || !double.IsFinite(currentTick))
                return false;

            if (player.tempo > 0)
            {
                if (currentTick >= previousTick)
                    return map.TryGetSavePopupCrossing(previousTick, currentTick, out _);

                if (!TryGetLoopTickRange(player, file, out double loopStartTick, out double loopEndTick))
                    return false;

                double lastTickBeforeLoopEnd = Math.Ceiling(loopEndTick) - 1;
                double firstTickAfterLoopStart = Math.Ceiling(loopStartTick);
                return (previousTick < lastTickBeforeLoopEnd && map.TryGetSavePopupCrossing(previousTick, lastTickBeforeLoopEnd, out _)) ||
                    (firstTickAfterLoopStart <= currentTick && map.TryGetSavePopupCrossing(firstTickAfterLoopStart - 1, currentTick, out _));
            }

            if (player.tempo < 0)
            {
                if (currentTick <= previousTick)
                    return map.TryGetSavePopupCrossing(previousTick, currentTick, out _);

                if (!TryGetLoopTickRange(player, file, out double loopStartTick, out double loopEndTick))
                    return false;

                double firstTickAfterLoopStart = Math.Floor(loopStartTick) + 1;
                double lastTickAtLoopEnd = Math.Floor(loopEndTick);
                return (previousTick > firstTickAfterLoopStart && map.TryGetSavePopupCrossing(previousTick, firstTickAfterLoopStart, out _)) ||
                    (lastTickAtLoopEnd >= currentTick && map.TryGetSavePopupCrossing(lastTickAtLoopEnd + 1, currentTick, out _));
            }

            return false;
        }

        static bool TryGetLoopTickRange(NBSPlayer player, NBSFile file, out double loopStartTick, out double loopEndTick)
        {
            loopStartTick = 0;
            loopEndTick = 0;
            if (!player.loop)
                return false;

            bool useFileLoop = player.useFileLoopSettings && file.header.loopEnabled;
            double loopStartTime = useFileLoop ? file.tempoMap.TickToTime(file.header.loopStartTick) : player.loopStart;
            double loopEndTime = useFileLoop
                ? file.duration
                : Math.Min(Math.Max(player.loopEnd, loopStartTime), file.duration);
            if (!double.IsFinite(loopStartTime) || !double.IsFinite(loopEndTime) || loopEndTime <= loopStartTime)
                return false;

            loopStartTick = useFileLoop ? file.header.loopStartTick : file.tempoMap.TimeToTick(loopStartTime);
            loopEndTick = useFileLoop ? file.tickLength : file.tempoMap.TimeToTick(loopEndTime);
            return double.IsFinite(loopStartTick) && double.IsFinite(loopEndTick) && loopEndTick > loopStartTick;
        }

        static Color GetAccentColor(NBSVisualEffectMap.State state)
        {
            if (state.rainbowEnabled)
            {
                float hue = (float)((EditorApplication.timeSinceStartup / 3d) % 1d);
                return Color.HSVToRGB(hue, 1, 1);
            }

            return state.mainColor ?? defaultAccentColor;
        }

        static string GetVisualEffectStatus(NBSVisualEffectMap.State state)
        {
            List<string> status = [];
            if (state.rainbowEnabled)
                status.Add(GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.rainbow"));
            if (state.backgroundAccentEnabled)
                status.Add(GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.background_accent"));
            if (state.mainColor is { } mainColor)
                status.Add($"#{mainColor.r:X2}{mainColor.g:X2}{mainColor.b:X2}");

            return status.Count > 0
                ? string.Join(" · ", status)
                : GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.inactive");
        }

        GUIStyle GetVisualEffectStatusStyle() => visualEffectStatusStyle ??= new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip
        };

        void DrawSavePopup(Rect cardPosition)
        {
            double elapsed = EditorApplication.timeSinceStartup - savePopupStartedAt;
            double totalDuration = savePopupHoldDuration + savePopupFadeDuration;
            if (elapsed < 0 || elapsed >= totalDuration)
                return;

            float alpha = elapsed <= savePopupHoldDuration
                ? 1
                : (float)(1 - ((elapsed - savePopupHoldDuration) / savePopupFadeDuration));

            GUIStyle style = GetSavePopupStyle();
            GUIContent content = TempContent(GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.song_saved"));
            Vector2 contentSize = style.CalcSize(content);
            float popupWidth = Mathf.Min(Mathf.Max(0, cardPosition.width - 16), contentSize.x + 16);
            float popupHeight = Mathf.Min(Mathf.Max(0, cardPosition.height - 8), EditorGUIUtility.singleLineHeight + 4);
            Rect popupPosition = new Rect
            (
                cardPosition.center.x - (popupWidth * 0.5f),
                cardPosition.center.y - (popupHeight * 0.5f),
                popupWidth,
                popupHeight
            );
            EditorGUI.DrawRect(popupPosition, new Color(0, 0, 0, 0.6f * alpha));

            float textWidth = Mathf.Min(contentSize.x, popupPosition.width);
            float textHeight = Mathf.Min(contentSize.y, popupPosition.height);
            Rect textPosition = new Rect
            (
                popupPosition.center.x - (textWidth * 0.5f),
                popupPosition.center.y - (textHeight * 0.5f),
                textWidth,
                textHeight
            );

            GUI.Label(textPosition, TempContent($"<color=#FFFFFFFF>{content.text}</color>"), style);
        }

        GUIStyle GetSavePopupStyle()
        {
            if (savePopupStyle != null)
                return savePopupStyle;

            savePopupStyle = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                richText = true
            };
            return savePopupStyle;
        }

        void ResetSavePopupTracking()
        {
            savePopupTrackers.Clear();
            savePopupStartedAt = double.NegativeInfinity;
        }

        async UniTask ReloadAndRepaint(NBSPlayer player)
        {
            await player.Reload();
            if (this != null)
                Repaint();
        }

        sealed class SavePopupTracker(NBSFile? file, double tick, bool wasPlaying)
        {
            public NBSFile? file = file;
            public double tick = tick;
            public bool wasPlaying = wasPlaying;
        }
    }
}
