using RuniOS.Linq;
using RuniOS.Sounds;

namespace RuniOS.Editor.IMGUI.Sounds
{
    public class GenericTimeUnit : ITimeUnit<ISeekable>
    {
        public void DrawField(Rect position, IEnumerable<ISeekable> timeables)
        {
            EditorGUI.showMixedValue = timeables.IsEmpty() || timeables.TwoOrMore();

            EditorGUI.BeginChangeCheck();
            double value = EditorGUI.DoubleField(position, TrTempContent("runios-editor:gui.second"), timeables.FirstOrDefault()?.time ?? 0);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var playable in timeables)
                    playable.time = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        public string TimeToString(ISeekable? timeable) => TimeUtility.ToTimeString(timeable?.time ?? double.NaN);
        public string RemainingTimeToString(ISeekable? timeable) => TimeUtility.ToTimeString((timeable?.length ?? double.NaN) - (timeable?.time ?? double.NaN));
        public string LengthToString(ISeekable? timeable) => TimeUtility.ToTimeString(timeable?.length ?? double.NaN);
    }
}