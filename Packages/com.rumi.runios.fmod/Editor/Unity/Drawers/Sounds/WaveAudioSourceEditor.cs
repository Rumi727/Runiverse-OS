#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(WaveAudioSource), true)]
    public class WaveAudioSourceEditor : RuniAudioSourceEditor<WaveAudioSource>
    {
        public WaveAudioSourceEditor() => playableController.timeUnits.Add(new WaveAudioTimeUnit());

        protected override bool DrawSourceLayout()
        {
            GUILayout.Label(GetTextOrKey("runios-editor:inspector.wave_audio_source.source.header"), EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            SerializedProperty? clipProperty = DrawPropertyLayout("_clipRef", TrTempContent("runios-editor:inspector.wave_audio_source.source.clip"));
            if (EditorGUI.EndChangeCheck() && clipProperty != null)
                ForEach(x => ReloadAndRepaint(x).Forget());

            DrawPropertyLayout("_nonRigidbodyVelocity", TrTempContent("runios-editor:inspector.wave_audio_source.source.non_rigidbody_velocity", "runios-editor:inspector.wave_audio_source.source.non_rigidbody_velocity.tooltip"));
            return true;
        }

        protected override void DrawAdditionalSpatialLayout()
        {
            DrawPropertyLayoutAndSync
            (
                "_rolloffMode",
                TrTempContent("runios-editor:inspector.wave_audio_source.spatial.rolloff_mode"),
                static x => x.rolloffMode,
                static (x, value) => x.rolloffMode = value
            );
        }

        protected override void DrawAdditionalInformationLayout()
        {
            DrawReadOnlyValue
            (
                TrTempContent("runios-editor:inspector.wave_audio_source.information.samples"),
                static x => x.samples,
                static (label, value) => { EditorGUILayout.LongField(label, value); }
            );
            DrawReadOnlyValue
            (
                TrTempContent("runios-editor:inspector.wave_audio_source.information.frequency"),
                static x => x.frequency,
                static (label, value) => { EditorGUILayout.FloatField(label, value); }
            );
        }

        async UniTask ReloadAndRepaint(WaveAudioSource item)
        {
            await item.Reload();

            if (this != null)
                Repaint();
        }
    }
}
