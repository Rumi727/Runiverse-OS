#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A scale with evenly spaced lines. The spacing doubles or halves with the zoom so lines stay at least <see cref="minimumSpacing"/> pixels apart.<br/>
    /// 같은 간격으로 선을 두는 스케일입니다. 선 사이가 <see cref="minimumSpacing"/> 픽셀 이상이 되도록 확대 수준에 따라 간격을 2배씩 늘리거나 줄입니다.
    /// </summary>
    /// <remarks>
    /// Example: with an axis in NBS ticks, <c>unit = 4</c> and <c>majorInterval = 4</c> shows beats with a strong line on every measure.<br/>
    /// 예: 축 단위가 NBS 틱이면 <c>unit = 4</c>, <c>majorInterval = 4</c>로 비트를 표시하고 마디마다 강한 선을 그립니다.
    /// </remarks>
    public class LinearGridScale : GridScale
    {
        // Above this many lines in one call, the request is treated as broken input and nothing is drawn.
        const long maximumLineCount = 100_000;

        /// <summary>
        /// Gets or sets how many axis units one step of this scale is. Labels count in steps.<br/>
        /// 이 스케일의 한 칸이 축 단위 몇 개인지 가져오거나 설정합니다. 라벨은 칸 수를 셉니다.
        /// </summary>
        public double unit { get; set; } = 1;

        /// <summary>
        /// Gets or sets the axis position of step zero.<br/>
        /// 0번째 칸의 축 위치를 가져오거나 설정합니다.
        /// </summary>
        public double origin { get; set; }

        /// <summary>
        /// Gets or sets every how many steps a level-0 line is drawn, or <c>0</c> for none.<br/>
        /// 몇 칸마다 0단계 선을 그릴지 가져오거나 설정합니다. <c>0</c>이면 그리지 않습니다.
        /// </summary>
        public double majorInterval { get; set; }

        /// <summary>
        /// Gets or sets how many level-2 parts each labeled spacing is split into, or <c>1</c> or less for none.<br/>
        /// 라벨이 붙는 간격 하나를 2단계 선으로 몇 등분할지 가져오거나 설정합니다. <c>1</c> 이하면 나누지 않습니다.
        /// </summary>
        public int subdivision { get; set; } = 4;

        /// <summary>
        /// Gets or sets the smallest pixel distance between labeled lines.<br/>
        /// 라벨이 붙는 선 사이의 최소 픽셀 거리를 가져오거나 설정합니다.
        /// </summary>
        public double minimumSpacing { get; set; } = 55;

        /// <summary>
        /// Gets or sets the smallest pixel distance between level-2 lines. Below it, level-2 lines are not drawn.<br/>
        /// 2단계 선 사이의 최소 픽셀 거리를 가져오거나 설정합니다. 이보다 좁으면 2단계 선을 그리지 않습니다.
        /// </summary>
        public double minimumSubdivisionSpacing { get; set; } = 8;

        /// <summary>
        /// Gets or sets a function that turns a step count into label text. When <see langword="null"/>, the count is written with up to three decimals.<br/>
        /// 칸 수를 라벨 글자로 바꾸는 함수를 가져오거나 설정합니다. <see langword="null"/>이면 소수 셋째 자리까지 씁니다.
        /// </summary>
        public Func<double, string>? labelFormatter { get; set; }

        /// <summary>
        /// Gets the axis distance between labeled lines at <paramref name="pixelsPerUnit"/>.<br/>
        /// <paramref name="pixelsPerUnit"/>에서 라벨이 붙는 선 사이의 축 거리를 가져옵니다.
        /// </summary>
        /// <param name="pixelsPerUnit">
        /// How many pixels one axis unit takes.<br/>
        /// 축 1단위가 차지하는 픽셀 수입니다.
        /// </param>
        /// <returns>
        /// <see cref="unit"/> multiplied by a power of two.<br/>
        /// <see cref="unit"/>에 2의 거듭제곱을 곱한 값을 반환합니다.
        /// </returns>
        public double GetStep(double pixelsPerUnit) => unit * Math.Pow(2, Math.Ceiling(Math.Log2(minimumSpacing / (unit * pixelsPerUnit))));

        /// <summary>
        /// Gets the axis distance between the finest lines drawn at <paramref name="pixelsPerUnit"/>, which is also the snapping distance.<br/>
        /// <paramref name="pixelsPerUnit"/>에서 그려지는 가장 촘촘한 선 사이의 축 거리를 가져옵니다. 맞춤 간격이기도 합니다.
        /// </summary>
        /// <param name="pixelsPerUnit">
        /// How many pixels one axis unit takes.<br/>
        /// 축 1단위가 차지하는 픽셀 수입니다.
        /// </param>
        /// <returns>
        /// The step divided by <see cref="subdivision"/> if level-2 lines are drawn; otherwise, the step.<br/>
        /// 2단계 선을 그리면 칸 간격을 <see cref="subdivision"/>으로 나눈 값을, 그렇지 않으면 칸 간격을 반환합니다.
        /// </returns>
        public double GetFinestStep(double pixelsPerUnit)
        {
            double step = GetStep(pixelsPerUnit);
            return HasSubdivision(step, pixelsPerUnit) ? step / subdivision : step;
        }

        bool HasSubdivision(double step, double pixelsPerUnit) => subdivision > 1 && step / subdivision * pixelsPerUnit >= minimumSubdivisionSpacing;

        /// <inheritdoc/>
        public override void GetLines(GridRange range, double pixelsPerUnit, List<GridLine> lines)
        {
            double step = GetStep(pixelsPerUnit);
            if (!double.IsFinite(step) || step <= 0)
                return;

            bool subdivided = HasSubdivision(step, pixelsPerUnit);
            int parts = subdivided ? subdivision : 1;
            double finest = step / parts;

            double first = Math.Ceiling((range.start - origin) / finest);
            double last = Math.Floor((range.end - origin) / finest);
            if (!(last - first < maximumLineCount))
                return;

            for (long i = (long)first; i <= (long)last; i++)
            {
                double position = origin + (i * finest);
                if (subdivided && i % parts != 0)
                {
                    lines.Add(new GridLine(position, 2));
                    continue;
                }

                double steps = (position - origin) / unit;
                bool major = majorInterval > 0 && IsMultiple(steps, majorInterval);
                lines.Add(new GridLine(position, major ? 0 : 1, labelFormatter?.Invoke(steps) ?? steps.ToString("0.###")));
            }
        }

        static bool IsMultiple(double value, double interval)
        {
            double ratio = value / interval;
            return Math.Abs(ratio - Math.Round(ratio)) < 1e-9;
        }

        /// <summary>
        /// Rounds <paramref name="position"/> to the nearest of the finest lines drawn at <paramref name="pixelsPerUnit"/>.<br/>
        /// <paramref name="position"/>을 <paramref name="pixelsPerUnit"/>에서 그려지는 가장 촘촘한 선 중 가장 가까운 선으로 반올림합니다.
        /// </summary>
        /// <inheritdoc/>
        public override double Snap(double position, double pixelsPerUnit)
        {
            double finest = GetFinestStep(pixelsPerUnit);
            if (!double.IsFinite(finest) || finest <= 0)
                return position;

            return origin + (Math.Round((position - origin) / finest) * finest);
        }
    }
}
