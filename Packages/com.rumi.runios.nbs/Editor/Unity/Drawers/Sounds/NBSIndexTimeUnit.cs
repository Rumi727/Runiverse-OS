#nullable enable
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Linq;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    public sealed class NBSIndexTimeUnit : ITimeUnit<NoteBlockSource>
    {
        public void DrawField(Rect position, IEnumerable<NoteBlockSource> sources)
        {
            EditorGUI.showMixedValue = sources.IsEmpty() || sources.TwoOrMore();
            NoteBlockSource? player = sources.FirstOrDefault();

            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.IntField(position, TrTempContent("runios-editor:inspector.nbs_player.transport.index"), player != null ? player.index : 0);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (NoteBlockSource target in sources)
                    target.index = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        public string TimeToString(NoteBlockSource? source) => source != null ? source.index.ToString() : "—";
        public string RemainingTimeToString(NoteBlockSource? source) => source != null ? Math.Max(0, source.indexLength - source.index - 1).ToString() : "—";
        public string LengthToString(NoteBlockSource? source) => source != null ? source.indexLength.ToString() : "—";
    }
}