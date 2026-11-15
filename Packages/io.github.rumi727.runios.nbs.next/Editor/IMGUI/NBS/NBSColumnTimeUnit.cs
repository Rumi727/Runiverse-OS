#nullable enable
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Linq;
using RuniOS.NBS;

namespace RuniOS.Editor.IMGUI.NBS
{
    /// <summary>
    /// Shows and edits the playback position of <see cref="NoteBlockSource"/> as the index of the tick column that contains notes.<br/>
    /// <see cref="NoteBlockSource"/>의 재생 위치를 노트가 있는 tick 열의 인덱스로 표시하고 편집합니다.
    /// </summary>
    public sealed class NBSColumnTimeUnit : ITimeUnit<NoteBlockSource>
    {
        /// <inheritdoc/>
        public void DrawField(Rect position, IEnumerable<NoteBlockSource> sources)
        {
            NoteBlockSource? first = sources.FirstOrDefault();
            EditorGUI.showMixedValue = first == null || sources.TwoOrMore();

            EditorGUI.BeginChangeCheck();
            int value = EditorGUI.IntField(position, TrTempContent("runios-editor:inspector.nbs_player.transport.index"), first != null ? GetColumn(first) : 0);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (NoteBlockSource source in sources)
                {
                    if (source.loadedClip is { } clip && clip.map.columns.Length > 0)
                        source.tick = clip.map.columns[Math.Clamp(value, 0, clip.map.columns.Length - 1)].tick;
                }
            }

            EditorGUI.showMixedValue = false;
        }

        /// <inheritdoc/>
        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        /// <inheritdoc/>
        public string TimeToString(NoteBlockSource? source) => source != null ? GetColumn(source).ToString() : "—";

        /// <inheritdoc/>
        public string RemainingTimeToString(NoteBlockSource? source) => source?.loadedClip is { } clip ? (clip.map.columns.Length - GetColumn(source)).ToString() : "—";

        /// <inheritdoc/>
        public string LengthToString(NoteBlockSource? source) => source?.loadedClip is { } clip ? clip.map.columns.Length.ToString() : "—";

        // The column at or before the current tick, which is the last column the marker has passed.
        static int GetColumn(NoteBlockSource source)
        {
            if (source.loadedClip is not { } clip)
                return 0;

            double tick = source.tick;
            int index = clip.map.FindColumnAtOrAfter(tick);
            if (index < clip.map.columns.Length && clip.map.columns[index].tick == tick)
                return index;

            return Math.Max(0, index - 1);
        }
    }
}
