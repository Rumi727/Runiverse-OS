using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public class WaveAudioTimeUnit : ITimeUnit
    {
        public void DrawField(Rect position, IReadOnlyList<IPlayable> playables)
        {
            EditorGUI.showMixedValue = playables.Count != 1;

            WaveAudioSource? waveAudioSource = playables.FirstOrDefault() as WaveAudioSource;

            EditorGUI.BeginChangeCheck();
            uint value = EditorGUI.LongField(position, TrTempContent("runios-editor:gui.sample"), waveAudioSource != null ? waveAudioSource.timeSample : 0).ClampToUInt();
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var playable in playables.OfType<WaveAudioSource>())
                    playable.timeSample = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        public string TimeToString(IPlayable? playable)
        {
            WaveAudioSource? waveAudioSource = playable as WaveAudioSource;
            return waveAudioSource != null ? waveAudioSource.timeSample.ToString() : "—";
        }

        public string RemainingTimeToString(IPlayable? playable)
        {
            WaveAudioSource? waveAudioSource = playable as WaveAudioSource;
            return waveAudioSource != null ? (waveAudioSource.samples - waveAudioSource.timeSample).ToString() : "—";
        }

        public string LengthToString(IPlayable? playable)
        {
            WaveAudioSource? waveAudioSource = playable as WaveAudioSource;
            return waveAudioSource != null ? waveAudioSource.samples.ToString() : "—";
        }
    }
}