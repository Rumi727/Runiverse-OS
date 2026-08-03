#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.IMGUI;
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

            if (EditPropertyValue
            (
                "_clipRef",
                x => x.clipRef,
                (position, x) => RuniFields.AssetRefField(position, TrTempContent("runios-editor:inspector.wave_audio_source.source.clip"), x.clipRef),
                (x, value) => x.clipRef = value,
                x => RuniFields.GetAssetRefFieldHeight(TrTempContent("runios-editor:inspector.wave_audio_source.source.clip"), x.clipRef)
            ))
                ForEach(x => ReloadAndRepaint(x).Forget());

            EditPropertyValue
            (
                "_nonRigidbodyVelocity",
                x => x.nonRigidbodyVelocity,
                (position, x) => EditorGUI.Toggle
                (
                    position,
                    TrTempContent("runios-editor:inspector.wave_audio_source.source.non_rigidbody_velocity", "runios-editor:inspector.wave_audio_source.source.non_rigidbody_velocity.tooltip"),
                    x.nonRigidbodyVelocity
                ),
                (x, value) => x.nonRigidbodyVelocity = value
            );
            return true;
        }

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

        protected override void DrawAdditionalInformationLayout()
        {
            EditorGUILayout.LabelField(TrTempContent("runios-editor:inspector.wave_audio_source.information.samples"), new GUIContent(GetCommonValueString(x => x.samples)));
            EditorGUILayout.LabelField(TrTempContent("runios-editor:inspector.wave_audio_source.information.frequency"), new GUIContent(GetCommonValueString(x => x.frequency)));
        }

        async UniTask ReloadAndRepaint(WaveAudioSource item)
        {
            await item.Reload();

            if (this != null)
                Repaint();
        }
    }
}
