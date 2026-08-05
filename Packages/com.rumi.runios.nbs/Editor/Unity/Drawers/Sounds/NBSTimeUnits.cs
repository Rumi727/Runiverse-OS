#nullable enable
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Sounds;

namespace RuniOS.Editor.Unity.Drawers.Sounds
{
    sealed class NBSTickTimeUnit : ITimeUnit
    {
        public void DrawField(Rect position, IReadOnlyList<IPlayable> playables)
        {
            EditorGUI.showMixedValue = playables.Count != 1;
            NoteBlockSource? player = playables.FirstOrDefault() as NoteBlockSource;

            EditorGUI.BeginChangeCheck();
            double value = EditorGUI.DoubleField
            (
                position,
                TrTempContent("runios-editor:inspector.nbs_player.transport.tick"),
                player != null ? player.tick : 0
            );
            if (EditorGUI.EndChangeCheck())
            {
                foreach (NoteBlockSource target in playables.OfType<NoteBlockSource>())
                    target.tick = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;
        public string TimeToString(IPlayable? playable) => playable is NoteBlockSource player ? player.tick.ToString("0.###") : "—";
        public string RemainingTimeToString(IPlayable? playable) => playable is NoteBlockSource player ? (player.tickLength - player.tick).ToString("0.###") : "—";
        public string LengthToString(IPlayable? playable) => playable is NoteBlockSource player ? player.tickLength.ToString() : "—";
    }

    sealed class NBSIndexTimeUnit : ITimeUnit
    {
        public void DrawField(Rect position, IReadOnlyList<IPlayable> playables)
        {
            EditorGUI.showMixedValue = playables.Count != 1;
            NoteBlockSource? player = playables.FirstOrDefault() as NoteBlockSource;

            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.IntField
            (
                position,
                TrTempContent("runios-editor:inspector.nbs_player.transport.index"),
                player != null ? player.index : 0
            );
            if (EditorGUI.EndChangeCheck())
            {
                foreach (NoteBlockSource target in playables.OfType<NoteBlockSource>())
                    target.index = value;
            }

            EditorGUI.showMixedValue = false;
        }

        public float GetHeight() => EditorGUIUtility.singleLineHeight;
        public string TimeToString(IPlayable? playable) => playable is NoteBlockSource player ? player.index.ToString() : "—";
        public string RemainingTimeToString(IPlayable? playable) => playable is NoteBlockSource player ? Math.Max(0, player.indexLength - player.index - 1).ToString() : "—";
        public string LengthToString(IPlayable? playable) => playable is NoteBlockSource player ? player.indexLength.ToString() : "—";
    }
}
