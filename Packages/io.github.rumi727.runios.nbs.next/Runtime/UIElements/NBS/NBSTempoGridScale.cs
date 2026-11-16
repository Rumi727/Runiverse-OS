#nullable enable
using RuniOS.NBS;
using RuniOS.UIElements.Grids;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// A ruler scale that labels every tempo change of the song with its tempo, like the <c>BPM = </c> tags that Note Block Studio puts on its ruler for tempo changers.<br/>
    /// 곡의 모든 템포 변경에 템포를 표시하는 룰러 스케일입니다. Note Block Studio가 템포 체인저를 룰러에 <c>BPM = </c> 태그로 표시하는 것과 같습니다.
    /// </summary>
    public sealed class NBSTempoGridScale : GridScale
    {
        /// <summary>
        /// Initializes a scale that draws labels only.<br/>
        /// 라벨만 그리는 스케일을 초기화합니다.
        /// </summary>
        public NBSTempoGridScale() => drawLines = false;

        /// <summary>
        /// Gets or sets the tempo map to label, or <see langword="null"/> to label nothing. The initial tempo is not a change, so it gets no label.<br/>
        /// 표시할 템포 맵을 가져오거나 설정하며, 아무것도 표시하지 않으려면 <see langword="null"/>입니다. 초기 템포는 변경이 아니므로 라벨이 없습니다.
        /// </summary>
        public NBSTempoMap? tempo { get; set; }

        /// <inheritdoc/>
        public override void GetLines(GridRange range, double pixelsPerUnit, bool includeLabels, List<GridLine> lines)
        {
            if (tempo == null)
                return;

            ReadOnlySpan<NBSTempoChange> changes = tempo.changes.AsSpan();

            // The first change is the initial tempo. Find the first change that is not left of the range.
            int low = 1;
            int high = changes.Length;
            while (low < high)
            {
                int middle = (low + high) >> 1;
                if (changes[middle].tick < range.start)
                    low = middle + 1;
                else
                    high = middle;
            }

            for (int i = low; i < changes.Length && changes[i].tick <= range.end; i++)
                lines.Add(new GridLine(changes[i].tick, 1, $"BPM = {changes[i].beatsPerMinute:0.##}"));
        }
    }
}
