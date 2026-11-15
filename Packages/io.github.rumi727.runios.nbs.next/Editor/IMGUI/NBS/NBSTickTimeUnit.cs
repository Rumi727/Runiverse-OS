#nullable enable
using System.Linq;
using RuniOS.Editor.IMGUI.Sounds;
using RuniOS.Linq;
using RuniOS.NBS;

namespace RuniOS.Editor.IMGUI.NBS
{
    /// <summary>
    /// Shows and edits the playback position of <see cref="NoteBlockSource"/> in NBS ticks.<br/>
    /// <see cref="NoteBlockSource"/>의 재생 위치를 NBS tick 단위로 표시하고 편집합니다.
    /// </summary>
    public sealed class NBSTickTimeUnit : ITimeUnit<NoteBlockSource>
    {
        /// <inheritdoc/>
        public void DrawField(Rect position, IEnumerable<NoteBlockSource> sources)
        {
            NoteBlockSource? first = sources.FirstOrDefault();
            EditorGUI.showMixedValue = first == null || sources.TwoOrMore();

            EditorGUI.BeginChangeCheck();
            double value = EditorGUI.DoubleField(position, TrTempContent("runios-editor:inspector.nbs_player.transport.tick"), first != null ? first.tick : 0);
            if (EditorGUI.EndChangeCheck())
            {
                foreach (NoteBlockSource source in sources)
                    source.tick = value;
            }

            EditorGUI.showMixedValue = false;
        }

        /// <inheritdoc/>
        public float GetHeight() => EditorGUIUtility.singleLineHeight;

        /// <inheritdoc/>
        public string TimeToString(NoteBlockSource? source) => source != null ? source.tick.ToString("0.###") : "—";

        /// <inheritdoc/>
        public string RemainingTimeToString(NoteBlockSource? source) => source?.loadedClip is { } clip ? (clip.map.tickLength - source.tick).ToString("0.###") : "—";

        /// <inheritdoc/>
        public string LengthToString(NoteBlockSource? source) => source?.loadedClip is { } clip ? clip.map.tickLength.ToString() : "—";
    }
}
