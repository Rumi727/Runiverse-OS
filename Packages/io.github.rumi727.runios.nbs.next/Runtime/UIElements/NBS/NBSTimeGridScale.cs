#nullable enable
using RuniOS.NBS;
using RuniOS.UIElements.Grids;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// Labels a tick axis with the playback time of <see cref="tempo"/>. Because tempo changes, the labels are not evenly spaced in ticks.<br/>
    /// <see cref="tempo"/>의 재생 시간으로 틱 축에 라벨을 붙입니다. 템포가 바뀌므로 라벨은 틱 기준으로 고르게 놓이지 않습니다.
    /// </summary>
    /// <remarks>
    /// The interval is the smallest of a fixed set of second values whose average pixel distance in the visible range reaches <see cref="minimumSpacing"/>.<br/>
    /// It does not draw lines on layers by default.
    /// <br/><br/>
    /// 간격은 정해진 초 값 중, 보이는 구간의 평균 픽셀 거리가 <see cref="minimumSpacing"/> 이상인 가장 작은 값입니다.<br/>
    /// 기본적으로 레이어에는 선을 그리지 않습니다.
    /// </remarks>
    public sealed class NBSTimeGridScale : GridScale
    {
        static readonly double[] intervals = [0.1, 0.2, 0.5, 1, 2, 5, 10, 15, 30, 60, 120, 300, 600, 1800, 3600];

        const long maximumLineCount = 100_000;

        /// <summary>
        /// Initializes a scale that does not draw lines on layers.<br/>
        /// 레이어에 선을 그리지 않는 스케일을 초기화합니다.
        /// </summary>
        public NBSTimeGridScale() => drawLines = false;

        /// <summary>
        /// Gets or sets the tempo that converts between ticks and seconds, or <see langword="null"/> to show nothing.<br/>
        /// 틱과 초를 변환하는 템포를 가져오거나 설정하며, 아무것도 표시하지 않으려면 <see langword="null"/>입니다.
        /// </summary>
        public NBSTempoMap? tempo { get; set; }

        /// <summary>
        /// Gets or sets the smallest average pixel distance between labels.<br/>
        /// 라벨 사이의 최소 평균 픽셀 거리를 가져오거나 설정합니다.
        /// </summary>
        public double minimumSpacing { get; set; } = 70;

        /// <inheritdoc/>
        public override void GetLines(GridRange range, double pixelsPerUnit, bool includeLabels, List<GridLine> lines)
        {
            if (tempo == null)
                return;

            double startTick = Math.Max(0, range.start);
            if (!(range.end > startTick))
                return;

            double startTime = tempo.TickToTime(startTick);
            double endTime = tempo.TickToTime(range.end);
            if (!(endTime > startTime))
                return;

            double pixelsPerSecond = ((range.end - startTick) * pixelsPerUnit) / (endTime - startTime);

            double interval = intervals[^1];
            foreach (double candidate in intervals)
            {
                if (candidate * pixelsPerSecond >= minimumSpacing)
                {
                    interval = candidate;
                    break;
                }
            }

            double first = Math.Ceiling(startTime / interval);
            double last = Math.Floor(endTime / interval);
            if (!(last - first < maximumLineCount))
                return;

            int decimalPlaces = interval < 1 ? 1 : 0;
            for (long i = (long)first; i <= (long)last; i++)
            {
                double time = i * interval;
                lines.Add(new GridLine(tempo.TimeToTick(time), 1, includeLabels ? TimeUtility.ToTimeString(time, AlwayShowTimeUnit.minute, decimalPlaces) : null));
            }
        }
    }
}
