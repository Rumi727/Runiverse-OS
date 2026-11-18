#nullable enable
using RuniOS.UIElements.Grids;

namespace RuniOS.UIElements.NBS
{
    /// <summary>
    /// Places grid lines on whole NBS ticks like Note Block Studio: every tick, every beat of four ticks, and every bar.<br/>
    /// Lines that would be closer than <see cref="minimumSpacing"/> are left out, and bars are thinned out by powers of two when zoomed out.
    /// <br/><br/>
    /// Note Block Studio처럼 NBS의 정수 틱 위치에 격자선을 배치합니다. 틱마다, 네 틱인 박마다, 마디마다 선을 둡니다.<br/>
    /// <see cref="minimumSpacing"/>보다 좁아지는 선은 생략하고, 축소하면 마디 선을 2의 거듭제곱 간격으로 솎아냅니다.
    /// </summary>
    /// <remarks>
    /// Bars are level 0 and labeled with their tick, beats are level 1, and ticks are level 2.<br/>
    /// 마디는 단계 0이며 틱 번호를 라벨로 가지고, 박은 단계 1, 틱은 단계 2입니다.
    /// </remarks>
    public sealed class NBSTickGridScale : GridScale
    {
        const int ticksPerBeat = 4;

        // Above this many lines in one call, the request is treated as broken input and nothing is drawn.
        const long maximumLineCount = 100_000;

        /// <summary>
        /// Gets or sets the number of beats in a bar. Values below 1 are treated as 1.<br/>
        /// 한 마디의 박 수를 가져오거나 설정합니다. 1보다 작은 값은 1로 취급합니다.
        /// </summary>
        public int timeSignature { get; set; } = 4;

        /// <summary>
        /// Gets or sets the smallest pixel distance between drawn lines.<br/>
        /// 그리는 선 사이의 최소 픽셀 거리를 가져오거나 설정합니다.
        /// </summary>
        public double minimumSpacing { get; set; } = 6;

        /// <summary>
        /// Gets or sets the smallest pixel distance between labels.<br/>
        /// 라벨 사이의 최소 픽셀 거리를 가져오거나 설정합니다.
        /// </summary>
        public double minimumLabelSpacing { get; set; } = 60;

        /// <inheritdoc/>
        public override void GetLines(GridRange range, double pixelsPerUnit, bool includeLabels, List<GridLine> lines)
        {
            if (!(pixelsPerUnit > 0))
                return;

            long barStep = Math.Max(1, timeSignature) * ticksPerBeat;
            while (barStep * pixelsPerUnit < minimumSpacing)
                barStep *= 2;

            long labelStep = barStep;
            while (labelStep * pixelsPerUnit < minimumLabelSpacing)
                labelStep *= 2;

            long lineStep = pixelsPerUnit >= minimumSpacing ? 1 : ticksPerBeat * pixelsPerUnit >= minimumSpacing ? ticksPerBeat : barStep;

            double first = Math.Ceiling(Math.Max(0, range.start) / lineStep);
            double last = Math.Floor(range.end / lineStep);
            if (!(last - first < maximumLineCount))
                return;

            for (long i = (long)first; i <= (long)last; i++)
            {
                long tick = i * lineStep;
                if (tick % barStep == 0)
                    lines.Add(new GridLine(tick, 0, includeLabels && tick % labelStep == 0 ? tick.ToString() : null));
                else
                    lines.Add(new GridLine(tick, tick % ticksPerBeat == 0 ? 1 : 2));
            }
        }

        /// <inheritdoc/>
        public override double Snap(double position, double pixelsPerUnit) => Math.Round(position);
    }
}
