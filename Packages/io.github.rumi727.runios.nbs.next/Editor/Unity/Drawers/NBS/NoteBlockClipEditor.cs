#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Editor.NBS;
using RuniOS.NBS;
using RuniOS.Resource;

namespace RuniOS.Editor.Unity.Drawers.NBS
{
    /// <summary>
    /// Draws a summary of a <see cref="NoteBlockClip"/> and a preview player instead of its raw note lists.<br/>
    /// <see cref="NoteBlockClip"/>의 원시 노트 리스트 대신 요약과 미리듣기 플레이어를 그립니다.
    /// </summary>
    [CustomEditor(typeof(NoteBlockClip))]
    public sealed class NoteBlockClipEditor : UnityEditor.Editor
    {
        readonly PlaybackController controller = new PlaybackController(new GenericTimeUnit()) { features = PlaybackControllerFeatures.all & ~PlaybackControllerFeatures.loopRange };
        NBSPreviewTransport? preview;
        bool isCreatingPreview;

        void OnDisable() => DisposePreview();

        /// <inheritdoc/>
        public override bool RequiresConstantRepaint() => preview?.isPlaying == true;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            // Unity draws imported objects disabled. This inspector edits nothing, so it is enabled to keep the preview controls usable.
            bool wasEnabled = GUI.enabled;
            GUI.enabled = true;

            try
            {
                DrawInspector();
            }
            finally
            {
                GUI.enabled = wasEnabled;
            }
        }

        void DrawInspector()
        {
            if (target is not NoteBlockClip clip)
                return;

            NBSHeader header = clip.header;
            NBSMap map = clip.map;

            GUILayout.Label(header.name.Length > 0 ? header.name : clip.name, EditorStyles.boldLabel);
            DrawLabel("Author", header.author);
            DrawLabel("Original Author", header.originalAuthor);
            if (header.description.Length > 0)
                EditorGUILayout.HelpBox(header.description, MessageType.None);

            Space();

            DrawLabel("Format Version", header.version.ToString());
            DrawLabel("Length", $"{TimeUtility.ToTimeString(map.length)} · {map.tickLength} ticks");
            DrawLabel("Tempo", $"{header.ticksPerSecond:0.##} TPS · {header.ticksPerSecond * 15:0.##} BPM");
            DrawLabel("Tempo Changes", (map.tempo.changes.Length - 1).ToString());
            DrawLabel("Notes", $"{map.notes.Count} in {map.columns.Length} columns");
            DrawLabel("Layers", map.layers.Length.ToString());
            DrawLabel("Sound Stoppers", map.stoppers.Length.ToString());
            DrawLabel("Loop", header.loop ? $"from tick {header.loopStartTick} · max {(header.maxLoopCount == 0 ? "∞" : header.maxLoopCount.ToString())}" : "off");

            if (!map.instruments.IsEmpty)
            {
                Space();
                GUILayout.Label("Custom Instruments", EditorStyles.boldLabel);
                for (int i = 0; i < map.instruments.Length; i++)
                {
                    NBSInstrument instrument = map.instruments[i];
                    NBSInstrumentFunction function = instrument.function;
                    DrawLabel(instrument.name, function == NBSInstrumentFunction.none ? instrument.soundFile : function.ToString());
                }
            }

            Space();
            GUILayout.Label("Preview", EditorStyles.boldLabel);
            DrawPreview(clip);
        }

        void DrawPreview(NoteBlockClip clip)
        {
            if (preview == null)
            {
                using (new EditorGUI.DisabledScope(isCreatingPreview))
                {
                    if (GUILayout.Button(isCreatingPreview ? "Loading instruments..." : "Load Preview"))
                        CreatePreview(clip).Forget();
                }

                return;
            }

            controller.target = preview;
            controller.DrawLayout();

            EditorGUI.BeginChangeCheck();
            float tempo = EditorGUILayout.Slider("Tempo", preview.tempo, -3, 3);
            float pitch = EditorGUILayout.Slider("Pitch", preview.pitch, -3, 3);
            if (EditorGUI.EndChangeCheck())
            {
                preview.tempo = tempo;
                preview.pitch = pitch;
            }
        }

        async UniTaskVoid CreatePreview(NoteBlockClip clip)
        {
            isCreatingPreview = true;
            try
            {
                NBSPreviewTransport created = await NBSPreviewTransport.CreateAsync(clip, Identifier.defaultNamespace);
                if (this == null || target != clip)
                {
                    created.Dispose();
                    return;
                }

                DisposePreview();
                preview = created;
            }
            finally
            {
                isCreatingPreview = false;
                if (this != null)
                    Repaint();
            }
        }

        void DisposePreview()
        {
            preview?.Dispose();
            preview = null;
        }

        static void DrawLabel(string label, string value) => EditorGUILayout.LabelField(label, value);
    }
}
