#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// One line that a <see cref="GridScale"/> asks the grid to draw.<br/>
    /// <see cref="GridScale"/>가 그리드에 그리도록 요청하는 선 하나입니다.
    /// </summary>
    /// <param name="position">
    /// The axis position of the line.<br/>
    /// 선의 축 위치입니다.
    /// </param>
    /// <param name="level">
    /// The emphasis of the line. <c>0</c> is the strongest, such as a measure line; <c>1</c> is a regular line; <c>2</c> is a subdivision line.<br/>
    /// 선의 강조 단계입니다. <c>0</c>이 가장 강하며(예: 마디 선), <c>1</c>은 기본 선, <c>2</c>는 보조 선입니다.
    /// </param>
    /// <param name="label">
    /// The text shown on rulers at this line, or <see langword="null"/> for no text.<br/>
    /// 눈금자에서 이 선에 표시할 글자이며, 없으면 <see langword="null"/>입니다.
    /// </param>
    public readonly record struct GridLine(double position, int level, string? label = null);
}
