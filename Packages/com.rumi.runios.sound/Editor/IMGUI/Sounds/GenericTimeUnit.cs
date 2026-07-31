using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public class GenericTimeUnit : ITimeUnit
    {
        public void DrawField(Rect position, IReadOnlyList<IPlayable> playables)
        {
            EditorGUI.showMixedValue = playables.Count != 1;

            EditorGUI.BeginChangeCheck();
            double value = EditorGUI.DoubleField(position, TrTempContent("runios-editor:gui.second"), playables.FirstOrDefault()?.time ?? 0);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var playable in playables)
                    playable.time = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        public string TimeToString(IPlayable? playable) => TimeUtility.ToTimeString(playable?.time ?? double.NaN);
        public string RemainingTimeToString(IPlayable? playable) => TimeUtility.ToTimeString((playable?.length ?? double.NaN) - (playable?.time ?? double.NaN));
        public string LengthToString(IPlayable? playable) => TimeUtility.ToTimeString(playable?.length ?? double.NaN);
    }
}