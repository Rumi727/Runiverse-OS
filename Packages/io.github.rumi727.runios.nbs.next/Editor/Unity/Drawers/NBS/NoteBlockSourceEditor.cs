#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI;
using RuniOS.Editor.IMGUI.NBS;
using RuniOS.Editor.Unity.Drawers.Sounds;
using RuniOS.NBS;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.NBS
{
    /// <summary>
    /// Draws the clip, playback, timeline lanes, visual effect preview, and song information of <see cref="NoteBlockSource"/>.<br/>
    /// <see cref="NoteBlockSource"/>의 클립, 재생, 타임라인 레인, 시각 효과 미리보기, 곡 정보를 그립니다.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NoteBlockSource), true)]
    public sealed class NoteBlockSourceEditor : RuniAudioSourceEditor<NoteBlockSource>
    {
        const float visualEffectCardHeight = 28;
        const double savePopupHoldDuration = 1;
        const double savePopupFadeDuration = 0.25;
        static readonly Color defaultAccentColor = new Color32(0, 120, 212, byte.MaxValue);
        static readonly Color savePopupColor = new Color(0.4f, 0.4f, 0.4f, 0.95f);

        double savePopupStartedAt = double.NegativeInfinity;
        double previousTick = double.NaN;
        bool isRainbowShown;

        /// <summary>
        /// Initializes the editor with the tick and column time units and the timeline lanes.<br/>
        /// tick과 열 시간 단위, 타임라인 레인으로 에디터를 초기화합니다.
        /// </summary>
        public NoteBlockSourceEditor()
        {
            controller.timeUnits.Add(new NBSTickTimeUnit());
            controller.timeUnits.Add(new NBSColumnTimeUnit());

            controller.timelineLaneHeight = NBSTimelineLaneDrawer.height;
            controller.timelineLane = NBSTimelineLaneDrawer.Draw;
        }

        /// <inheritdoc/>
        protected override bool repaintInEditor => base.repaintInEditor || isRainbowShown || EditorApplication.timeSinceStartup < savePopupStartedAt + savePopupHoldDuration + savePopupFadeDuration;

        /// <inheritdoc/>
        protected override bool DrawSourceLayout()
        {
            GUILayout.Label(TrTempContent("runios-editor:inspector.nbs_player.source.header"), EditorStyles.boldLabel);

            if (EditPropertyValue
            (
                "_clip",
                x => x.clip,
                (position, x) => RuniFields.AssetRefField(position, TrTempContent("runios-editor:inspector.nbs_player.source.file"), x.clip),
                (x, value) => x.clip = value,
                x => RuniFields.GetAssetRefFieldHeight(TrTempContent("runios-editor:inspector.nbs_player.source.file"), x.clip)
            ))
                ForEach(x => ReloadAndRepaint(x).Forget());

            if (EditPropertyValue
            (
                "_directInstrumentNamespace",
                x => x.directInstrumentNamespace,
                (position, x) => EditorGUI.TextField(position, TrTempContent("runios-editor:inspector.nbs_player.source.custom_instrument_namespace"), x.directInstrumentNamespace),
                (x, value) => x.directInstrumentNamespace = value
            ))
                ForEach(x => ReloadAndRepaint(x).Forget());

            EditPropertyValue
            (
                "_useFileLoopSettings",
                x => x.useFileLoopSettings,
                (position, x) => EditorGUI.Toggle(position, TrTempContent("runios-editor:inspector.nbs_player.source.use_file_loop_settings"), x.useFileLoopSettings),
                (x, value) => x.useFileLoopSettings = value
            );

            EditPropertyValue
            (
                "_reversePitchAlignment",
                x => x.reversePitchAlignment,
                (position, x) => (NBSReversePitchAlignment)EditorGUI.EnumPopup
                (
                    position,
                    TrTempContent
                    (
                        "runios-editor:inspector.nbs_player.source.reverse_pitch_alignment",
                        "runios-editor:inspector.nbs_player.source.reverse_pitch_alignment.tooltip"
                    ),
                    x.reversePitchAlignment
                ),
                (x, value) => x.reversePitchAlignment = value
            );

            return true;
        }

        /// <inheritdoc/>
        protected override void DrawPlaybackLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.playback.header"), EditorStyles.boldLabel);

            EditPropertyValue
            (
                "_volume",
                x => x.volume,
                (position, x) => EditorGUI.Slider(position, TrTempContent("runios-editor:inspector.runi_audio_source.playback.volume"), x.volume, 0, 2),
                (x, value) => x.volume = value
            );
            EditPropertyValue
            (
                "_tempo",
                x => x.tempo,
                (position, x) => EditorGUI.Slider
                (
                    position,
                    TrTempContent("runios-editor:inspector.runi_audio_source.playback.tempo", "runios-editor:inspector.nbs_player.playback.tempo.tooltip"),
                    x.tempo,
                    -3,
                    3
                ),
                (x, value) => x.tempo = value
            );
            EditPropertyValue
            (
                "_pitch",
                x => x.pitch,
                (position, x) => EditorGUI.Slider
                (
                    position,
                    TrTempContent("runios-editor:inspector.runi_audio_source.playback.pitch", "runios-editor:inspector.nbs_player.playback.pitch.tooltip"),
                    x.pitch,
                    -3,
                    3
                ),
                (x, value) => x.pitch = value
            );
        }

        /// <inheritdoc/>
        protected override void DrawAdditionalSpatialLayout()
        {
            EditPropertyValue
            (
                "_rolloffMode",
                x => x.rolloffMode,
                (position, x) => (SoundRolloffMode)EditorGUI.EnumPopup(position, TrTempContent("runios-editor:inspector.wave_audio_source.spatial.rolloff_mode"), x.rolloffMode),
                (x, value) => x.rolloffMode = value
            );
        }

        /// <inheritdoc/>
        protected override void DrawTransportLayout()
        {
            base.DrawTransportLayout();
            Space();

            EditorGUI.BeginChangeCheck();
            double interval = EditorGUILayout.DoubleField
            (
                TrTempContent("runios-editor:inspector.nbs_player.transport.worker_interval", "runios-editor:inspector.nbs_player.transport.worker_interval.tooltip"),
                NBSPlaybackSettings.workerInterval
            );
            if (EditorGUI.EndChangeCheck() && double.IsFinite(interval) && interval > 0)
                NBSPlaybackSettings.workerInterval = interval;

            EditorGUI.BeginChangeCheck();
            double lookahead = EditorGUILayout.DoubleField
            (
                TrTempContent("runios-editor:inspector.nbs_player.transport.scheduling_lookahead", "runios-editor:inspector.nbs_player.transport.scheduling_lookahead.tooltip"),
                NBSPlaybackSettings.schedulingLookahead
            );
            if (EditorGUI.EndChangeCheck() && double.IsFinite(lookahead) && lookahead >= 0)
                NBSPlaybackSettings.schedulingLookahead = lookahead;
        }

        /// <inheritdoc/>
        protected override void DrawInformationLayout()
        {
            if (DrawVisualEffectsLayout())
                Space();

            base.DrawInformationLayout();
        }

        /// <inheritdoc/>
        protected override void DrawAdditionalInformationLayout()
        {
            DrawInformation("runios-editor:inspector.nbs_player.information.tick_length", x => x.loadedClip?.map.tickLength);
            DrawInformation("runios-editor:inspector.nbs_player.information.index_length", x => x.loadedClip?.map.columns.Length);
            DrawInformation("runios-editor:inspector.nbs_player.information.tps", x => x.loadedClip?.map.tempo.GetChangeAtTick(x.tick).ticksPerSecond.ToString("0.##"));
            DrawInformation("runios-editor:inspector.nbs_player.information.bpm", x => x.loadedClip?.map.tempo.GetChangeAtTick(x.tick).beatsPerMinute.ToString("0.##"));
            DrawInformation("runios-editor:inspector.nbs_player.information.custom_instruments", x => x.loadedClip?.map.instruments.Length);
        }

        void DrawInformation<TValue>(string key, Func<NoteBlockSource, TValue> readFunc) => EditorGUILayout.LabelField(TrTempContent(key), new GUIContent(GetCommonValueString(readFunc)));

        bool DrawVisualEffectsLayout()
        {
            isRainbowShown = false;
            if (targets.Length != 1 || targets[0] is not { } source || source.loadedClip is not { } clip)
                return false;

            NBSVisualEffectMap effects = clip.map.visualEffects;
            if (effects.states.IsEmpty && effects.savePopupTicks.IsEmpty)
                return false;

            double tick = source.tick;
            if (double.IsFinite(previousTick) && effects.TryFindSavePopup(previousTick, tick, out _))
                savePopupStartedAt = EditorApplication.timeSinceStartup;

            previousTick = tick;

            GUILayout.Label(TrTempContent("runios-editor:inspector.nbs_player.visual_effects.header"), EditorStyles.boldLabel);

            NBSVisualEffectState state = effects.GetState(tick);
            isRainbowShown = state.rainbow;
            Color accent = state.color is { } color ? (Color)color : defaultAccentColor;
            if (state.rainbow)
                accent = Color.HSVToRGB((float)(EditorApplication.timeSinceStartup * 0.25 % 1), 0.7f, 0.95f);

            Rect card = EditorGUILayout.GetControlRect(false, visualEffectCardHeight);
            GUI.Box(card, GUIContent.none, EditorStyles.helpBox);
            if (state.backgroundAccent)
                EditorGUI.DrawRect(new Rect(card.x + 1, card.y + 1, card.width - 2, card.height - 2), accent with { a = 0.18f });

            EditorGUI.DrawRect(new Rect(card.x + 1, card.y + 1, 4, card.height - 2), accent);

            GUI.Label(new Rect(card.x + 12, card.y, card.width - 16, card.height), GetStatus(state), EditorStyles.label);
            DrawSavePopup(card);

            return true;
        }

        static string GetStatus(NBSVisualEffectState state)
        {
            List<string> status = [];
            if (state.rainbow)
                status.Add(GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.rainbow"));
            if (state.backgroundAccent)
                status.Add(GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.background_accent"));
            if (state.color is { } color)
                status.Add($"#{color.r:X2}{color.g:X2}{color.b:X2}");

            return status.Count > 0
                ? string.Join(" · ", status)
                : GetTextOrKey("runios-editor:inspector.nbs_player.visual_effects.inactive");
        }

        /// <summary>
        /// Draws the song saved popup floating over the center of the visual effect card, fading out after it is held.<br/>
        /// 노래 저장 팝업을 시각 효과 카드 가운데 위에 띄워 그리며, 잠시 유지한 뒤 서서히 사라지게 합니다.
        /// </summary>
        void DrawSavePopup(Rect card)
        {
            double elapsed = EditorApplication.timeSinceStartup - savePopupStartedAt;
            if (elapsed >= savePopupHoldDuration + savePopupFadeDuration)
                return;

            float alpha = elapsed <= savePopupHoldDuration ? 1 : (float)(1 - ((elapsed - savePopupHoldDuration) / savePopupFadeDuration));

            GUIContent content = TrTempContent("runios-editor:inspector.nbs_player.visual_effects.song_saved");
            Vector2 size = EditorStyles.whiteLabel.CalcSize(content);
            Rect popup = new Rect(card.center.x - (size.x * 0.5f) - 8, card.center.y - (size.y * 0.5f) - 1, size.x + 16, size.y + 2);
            GUI.DrawTexture(popup, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, savePopupColor with { a = savePopupColor.a * alpha }, 0, 4);

            Color previousColor = GUI.color;
            GUI.color = previousColor with { a = previousColor.a * alpha };
            GUI.Label(new Rect(popup.x + 8, popup.y + 1, size.x, size.y), content, EditorStyles.whiteLabel);
            GUI.color = previousColor;
        }

        async UniTask ReloadAndRepaint(NoteBlockSource source)
        {
            await source.Reload();
            if (this != null)
                Repaint();
        }
    }
}
