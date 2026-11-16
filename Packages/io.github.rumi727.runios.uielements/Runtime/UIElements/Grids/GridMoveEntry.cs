#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// One item moved by dragging, with where it was before the drag.<br/>
    /// 끌어서 옮긴 아이템 하나와 끌기 전 위치입니다.
    /// </summary>
    /// <param name="item">
    /// The moved item.<br/>
    /// 옮긴 아이템입니다.
    /// </param>
    /// <param name="originalStart">
    /// <see cref="GridItem.start"/> before the drag.<br/>
    /// 끌기 전의 <see cref="GridItem.start"/>입니다.
    /// </param>
    /// <param name="originalTop">
    /// <see cref="PlaneGridItem.top"/> before the drag, or <c>0</c> for other items.<br/>
    /// 끌기 전의 <see cref="PlaneGridItem.top"/>이며, 다른 아이템은 <c>0</c>입니다.
    /// </param>
    public readonly record struct GridMoveEntry(GridItem item, double originalStart, double originalTop);
}
