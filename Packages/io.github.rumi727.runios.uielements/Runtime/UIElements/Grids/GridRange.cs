#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A closed range on a grid axis. The unit is whatever the axis uses.<br/>
    /// 그리드 축 위의 닫힌 범위입니다. 단위는 축이 쓰는 단위를 그대로 따릅니다.
    /// </summary>
    /// <param name="start">
    /// The start of the range.<br/>
    /// 범위의 시작입니다.
    /// </param>
    /// <param name="end">
    /// The end of the range.<br/>
    /// 범위의 끝입니다.
    /// </param>
    public readonly record struct GridRange(double start, double end)
    {
        /// <summary>
        /// Gets the length of the range, <c>end - start</c>.<br/>
        /// 범위의 길이 <c>end - start</c>를 가져옵니다.
        /// </summary>
        public double length => end - start;

        /// <summary>
        /// Determines whether <paramref name="position"/> is inside the range, including both ends.<br/>
        /// <paramref name="position"/>이 양 끝을 포함한 범위 안에 있는지 확인합니다.
        /// </summary>
        /// <param name="position">
        /// The position to check.<br/>
        /// 확인할 위치입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the position is inside the range; otherwise, <see langword="false"/>.<br/>
        /// 위치가 범위 안에 있으면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Contains(double position) => position >= start && position <= end;

        /// <summary>
        /// Determines whether this range and <paramref name="other"/> share at least one position.<br/>
        /// 이 범위와 <paramref name="other"/>가 한 위치라도 공유하는지 확인합니다.
        /// </summary>
        /// <param name="other">
        /// The range to compare with.<br/>
        /// 비교할 범위입니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the ranges overlap; otherwise, <see langword="false"/>.<br/>
        /// 범위가 겹치면 <see langword="true"/>를 반환하고, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public bool Overlaps(GridRange other) => start <= other.end && other.start <= end;
    }
}
