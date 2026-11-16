#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// The result of moving items by dragging. The items already have their new values; use it to update your data or record undo.<br/>
    /// 아이템을 끌어서 옮긴 결과입니다. 아이템에는 이미 새 값이 들어 있으므로, 데이터를 갱신하거나 실행 취소를 기록하는 데 쓰세요.
    /// </summary>
    /// <param name="entries">
    /// The moved items with their original positions.<br/>
    /// 옮긴 아이템과 원래 위치입니다.
    /// </param>
    /// <param name="delta">
    /// The axis distance every item moved.<br/>
    /// 모든 아이템이 옮겨진 축 거리입니다.
    /// </param>
    /// <param name="verticalDelta">
    /// The vertical distance every <see cref="PlaneGridItem"/> moved.<br/>
    /// 모든 <see cref="PlaneGridItem"/>이 옮겨진 세로 거리입니다.
    /// </param>
    public readonly record struct GridMoveResult(IReadOnlyList<GridMoveEntry> entries, double delta, double verticalDelta);
}
