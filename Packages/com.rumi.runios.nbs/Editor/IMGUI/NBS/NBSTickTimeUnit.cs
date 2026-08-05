#nullable enable
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Linq;
using RuniOS.NBS;

namespace RuniOS.Editor.IMGUI.NBS
{
    public sealed class NBSTickTimeUnit : ITimeUnit<NoteBlockSource>
    {
        public void DrawField(Rect position, IEnumerable<NoteBlockSource> sources)
        {
            EditorGUI.showMixedValue = sources.IsEmpty() || sources.TwoOrMore();
            NoteBlockSource? player = sources.FirstOrDefault();

            EditorGUI.BeginChangeCheck();
            double value = EditorGUI.DoubleField(position, TrTempContent("runios-editor:inspector.nbs_player.transport.tick"), player != null ? player.tick : 0);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (NoteBlockSource target in sources)
                    target.tick = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        public string TimeToString(NoteBlockSource? playable) => playable != null ? playable.tick.ToString("0.###") : "—";
        public string RemainingTimeToString(NoteBlockSource? playable) => playable != null ? (playable.tickLength - playable.tick).ToString("0.###") : "—";
        public string LengthToString(NoteBlockSource? playable) => playable != null ? playable.tickLength.ToString() : "—";
    }
}