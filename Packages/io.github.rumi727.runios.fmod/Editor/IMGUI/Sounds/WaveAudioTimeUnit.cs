using RuniOS.Linq;
using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public class WaveAudioTimeUnit : ITimeUnit<WaveAudioSource>
    {
        public void DrawField(Rect position, IEnumerable<WaveAudioSource> sources)
        {
            EditorGUI.showMixedValue = sources.IsEmpty() || sources.TwoOrMore();

            WaveAudioSource? source = sources.FirstOrDefault();

            EditorGUI.BeginChangeCheck();
            uint value = EditorGUI.LongField(position, TrTempContent("runios-editor:gui.sample"), source != null ? source.timeSample : 0).ClampToUInt();
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var playable in sources)
                    playable.timeSample = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        public string TimeToString(WaveAudioSource? source) => source != null ? source.timeSample.ToString() : "—";
        public string RemainingTimeToString(WaveAudioSource? source) => source != null ? (source.samples - source.timeSample).ToString() : "—";
        public string LengthToString(WaveAudioSource? source) => source != null ? source.samples.ToString() : "—";
    }
}