#nullable enable
using RuniOS.Editor.NBS;
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.NBS;
using RuniOS.Resource;

namespace RuniOS.Editor.Unity.Drawers.NBS
{
    /// <summary>
    /// Previews a <see cref="NoteBlockClip"/> in the preview area, like an audio clip: the song header as an overlay, a summary below it, and simple playback.<br/>
    /// 오디오 클립처럼 <see cref="NoteBlockClip"/>을 미리보기 영역에 표시합니다. 곡 헤더는 오버레이로, 요약은 그 아래에 표시하고 간단히 재생합니다.
    /// </summary>
    /// <remarks>
    /// The inspector body is empty, because <see cref="NoteBlockClipImporterEditor"/> does not show the imported object and the preview area shows the clip instead.<br/>
    /// <see cref="NoteBlockClipImporterEditor"/>가 가져온 오브젝트를 표시하지 않고 미리보기 영역이 클립을 대신 보여주므로 인스펙터 본문은 비어 있습니다.
    /// </remarks>
    [CustomEditor(typeof(NoteBlockClip))]
    public sealed class NoteBlockClipEditor : CustomInspectorBase<NoteBlockClip>
    {
        static GUIStyle overlayLabelStyle => _overlayLabelStyle ??= "PreOverlayLabel";
        static GUIStyle? _overlayLabelStyle;

        NBSPreview preview = null!;
        NBSPreview.Player? player;

        protected override void OnEnable()
        {
            base.OnEnable();

            preview = new NBSPreview(Identifier.defaultNamespace);
            preview.onBankLoadCompleted += OnBankLoadCompleted;

            if (NBSPreview.autoPlay && target != null)
                GetOrCreatePlayer(target).Play();
        }

        protected override void OnDisable()
        {
            preview.Dispose();
            player = null;
        }

        /// <inheritdoc/>
        public override void OnInspectorGUI() { }

        /// <inheritdoc/>
        public override bool HasPreviewGUI() => true;

        /// <inheritdoc/>
        public override void OnPreviewSettings()
        {
            if (target == null)
                return;

            // Without a loop the transport keeps running past the end, so the preview stops there.
            if (player is { isPlaying: true, loop: false } && player.time >= player.length)
                player.Stop();

            using (new EditorGUI.DisabledScope(player?.isLoading == true))
            {
                bool isPlaying = player?.isPlaying == true;
                if (GUILayout.Toggle(isPlaying, EditorGUIUtility.IconContent("Play"), EditorStyles.toolbarButton) != isPlaying)
                {
                    if (isPlaying)
                        player!.Stop();
                    else
                        GetOrCreatePlayer(target).Play();
                }
            }

            NBSPreview.autoPlay = GUILayout.Toggle(NBSPreview.autoPlay, EditorGUIUtility.IconContent("preAudioAutoPlayOff"), EditorStyles.toolbarButton);

            // The loop follows the file until the instruments are loaded.
            using (new EditorGUI.DisabledScope(player is not { isLoaded: true }))
            {
                bool loop = player?.loop ?? target.header.loop;
                if (GUILayout.Toggle(loop, EditorGUIUtility.IconContent("preAudioLoopOff"), EditorStyles.toolbarButton) != loop)
                    player!.loop = !loop;
            }

            Rect sliderRect = EditorGUILayout.GetControlRect(GUILayout.Width(75));
            EditorGUI.BeginChangeCheck();
            float volume = GUI.HorizontalSlider(sliderRect, NBSPreview.globalVolume, 0, 1);
            if (EditorGUI.EndChangeCheck())
                NBSPreview.globalVolume = volume;

            GUI.Box(sliderRect, TempContent("", $"{GetTextOrKey("runios-editor:gui.volume")}: {(NBSPreview.globalVolume * 100).Floor()}"), GUIStyle.none);
        }

        /// <inheritdoc/>
        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            if (target == null)
                return;

            if (Event.current.type == EventType.Repaint)
                background.Draw(r, false, false, false, false);

            Rect labelRect = r;
            labelRect.xMin += 3;

            BeginAlignment(TextAnchor.UpperLeft, overlayLabelStyle);
            EditorGUI.DropShadowLabel(labelRect, GetHeaderText(target));
            EndAlignment(overlayLabelStyle);

            if (player is not { isPlaying: true } || !(player.length > 0))
                return;

            double time = player.time;
            double position = time / player.length;
            if (Event.current.type == EventType.Repaint && position is >= 0 and <= 1)
                EditorGUI.DrawRect(new Rect(Mathf.Lerp(r.x - 1, r.xMax - 1, (float)position), r.y, 2, r.height), Color.white);

            labelRect.xMax -= 3;

            BeginRichText(overlayLabelStyle);
            BeginAlignment(TextAnchor.UpperRight, overlayLabelStyle);
            EditorGUI.DropShadowLabel(labelRect, RichNumberMSpace(TimeUtility.ToTimeString(time)));
            EndAlignment(overlayLabelStyle);
            EndRichText(overlayLabelStyle);

            // This editor is reached only through the importer's preview, so like an audio clip the preview requests its own repaint while playing.
            Repaint();
        }

        /// <inheritdoc/>
        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            OnPreviewGUI(r, background);

            Event evt = Event.current;
            if (player is { isPlaying: true } && evt.button == 0 && evt.type is EventType.MouseDown or EventType.MouseDrag && r.Contains(evt.mousePosition))
            {
                player.time = Mathf.InverseLerp(r.x, r.xMax, evt.mousePosition.x) * player.length;
                evt.Use();
            }
        }

        /// <inheritdoc/>
        public override string GetInfoString()
        {
            if (target == null)
                return string.Empty;

            NBSHeader header = target.header;
            NBSMap map = target.map;

            string loop = header.loop ? $"loop from tick {header.loopStartTick} · max {(header.maxLoopCount == 0 ? "∞" : header.maxLoopCount.ToString())}" : "no loop";
            return $"Format {header.version} · {TimeUtility.ToTimeString(map.length)} · {map.tickLength} ticks · {header.ticksPerSecond:0.##} TPS · {header.ticksPerSecond * 15:0.##} BPM · "
                + $"{map.tempo.changes.Length - 1} tempo changes · {map.notes.Count} notes in {map.columns.Length} columns · {map.layers.Length} layers · "
                + $"{map.stoppers.Length} sound stoppers · {map.instruments.Length} custom instruments · {loop}";
        }

        static string GetHeaderText(NoteBlockClip clip)
        {
            NBSHeader header = clip.header;

            string text = header.name.Length > 0 ? header.name : clip.name;
            if (header.author.Length > 0)
                text += $"\nAuthor: {header.author}";
            if (header.originalAuthor.Length > 0)
                text += $"\nOriginal Author: {header.originalAuthor}";
            if (header.description.Length > 0)
                text += $"\n\n{header.description}";

            return text;
        }

        NBSPreview.Player GetOrCreatePlayer(NoteBlockClip clip) => player ??= preview.GetOrCreatePlayer(clip);

        void OnBankLoadCompleted(NoteBlockClip clip) => Repaint();
    }
}
