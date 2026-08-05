#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Editor.Unity.Inspectors;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    public abstract class RuniAudioSourceEditor<TTarget> : CustomInspectorBase<TTarget> where TTarget : RuniAudioSource
    {
        public PlaybackController controller { get; } = new PlaybackController(new GenericTimeUnit());

        protected override bool repaintInEditor => controller.targets.Any(t => t.isPlaying);

        protected override void OnEnable()
        {
            base.OnEnable();
            controller.targets.SyncWithEnumerable(targets);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            if (DrawSourceLayout())
                Space();

            DrawPlaybackLayout();
            Space();

            DrawLoopingLayout();
            Space();

            DrawSpatialLayout();
            Space();

            DrawTransportLayout();
            Space();

            DrawInformationLayout();
        }

        protected virtual bool DrawSourceLayout() => false;

        protected virtual void DrawPlaybackLayout()
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
                (position, x) => EditorGUI.Slider(position, TrTempContent("runios-editor:inspector.runi_audio_source.playback.tempo"), x.tempo, -3, 3),
                (x, value) => x.tempo = value
            );
            EditPropertyValue
            (
                "_pitch",
                x => x.pitch,
                (position, x) => EditorGUI.Slider(position, TrTempContent("runios-editor:inspector.runi_audio_source.playback.pitch"), x.pitch, 0, 3),
                (x, value) => x.pitch = value
            );
        }

        protected virtual void DrawLoopingLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.looping.header"), EditorStyles.boldLabel);

            EditPropertyValue
            (
                "_loop",
                x => x.loop,
                (position, x) => EditorGUI.Toggle(position, TrTempContent("runios-editor:inspector.runi_audio_source.looping.loop"), x.loop),
                (x, value) => x.loop = value
            );
            EditPropertyValue
            (
                "_loopStart",
                x => x.loopStart,
                (position, x) => EditorGUI.DoubleField(position, TrTempContent("runios-editor:inspector.runi_audio_source.looping.start"), x.loopStart),
                (x, value) => x.loopStart = value
            );
            EditPropertyValue
            (
                "_loopEnd",
                x => x.loopEnd,
                (position, x) => RuniFields.NullablePrimitiveField<double>
                (
                    position,
                    TrTempContent("runios-editor:inspector.runi_audio_source.looping.end"),
                    x.loopEnd.Approximately(double.MaxValue) ? null : x.loopEnd,
                    GetTextOrKey("runios-editor:gui.none")
                ) ?? double.MaxValue,
                (x, value) => x.loopEnd = value,
                _ => RuniFields.GetMultiColumnsFieldHeight(TrTempContent("runios-editor:inspector.runi_audio_source.looping.end"))
            );
        }

        protected virtual void DrawSpatialLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.spatial.header"), EditorStyles.boldLabel);

            EditPropertyValue
            (
                "_panStereo",
                x => x.panStereo,
                (position, x) => EditorGUI.Slider(position, TrTempContent("runios-editor:inspector.runi_audio_source.spatial.pan_stereo"), x.panStereo, -1, 1),
                (x, value) => x.panStereo = value
            );
            EditPropertyValue
            (
                "_spatialBlend",
                x => x.spatialBlend,
                (position, x) => EditorGUI.Slider(position, TrTempContent("runios-editor:inspector.runi_audio_source.spatial.blend"), x.spatialBlend, 0, 1),
                (x, value) => x.spatialBlend = value
            );
            EditPropertyValue
            (
                "_dopplerLevel",
                x => x.dopplerLevel,
                (position, x) => EditorGUI.Slider(position, TrTempContent("runios-editor:inspector.runi_audio_source.spatial.doppler_level"), x.dopplerLevel, 0, 5),
                (x, value) => x.dopplerLevel = value
            );
            EditPropertyValue
            (
                "_spread",
                x => x.spread,
                (position, x) => EditorGUI.Slider(position, TrTempContent("runios-editor:inspector.runi_audio_source.spatial.spread"), x.spread, 0, 360),
                (x, value) => x.spread = value
            );
            EditPropertyValue
            (
                "_minDistance",
                x => x.minDistance,
                (position, x) => EditorGUI.FloatField(position, TrTempContent("runios-editor:inspector.runi_audio_source.spatial.min_distance"), x.minDistance),
                (x, value) => x.minDistance = value
            );
            EditPropertyValue
            (
                "_maxDistance",
                x => x.maxDistance,
                (position, x) => EditorGUI.FloatField(position, TrTempContent("runios-editor:inspector.runi_audio_source.spatial.max_distance"), x.maxDistance),
                (x, value) => x.maxDistance = value
            );

            DrawAdditionalSpatialLayout();
        }

        protected virtual void DrawAdditionalSpatialLayout() { }

        protected virtual void DrawTransportLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.transport.header"), EditorStyles.boldLabel);
            controller.DrawLayout();
        }

        protected virtual void DrawInformationLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.runi_audio_source.information.header"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(TrTempContent("runios-editor:inspector.runi_audio_source.information.length"), new GUIContent(GetCommonValueString(x => x.length)));

            DrawAdditionalInformationLayout();
        }

        protected virtual void DrawAdditionalInformationLayout() { }
    }

    [CanEditMultipleObjects]
    [CustomEditor(typeof(RuniAudioSource), true)]
    class RuniAudioSourceDefaultEditor : RuniAudioSourceEditor<RuniAudioSource>;
}
