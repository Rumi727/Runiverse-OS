#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// An item of a <see cref="LaneGridLayer"/>. The layer picks its lane so that it does not overlap other items.<br/>
    /// <see cref="LaneGridLayer"/>의 아이템입니다. 다른 아이템과 겹치지 않도록 레이어가 레인을 고릅니다.
    /// </summary>
    public class LaneGridItem : GridItem
    {
        /// <summary>
        /// Gets the lane the layer put the item in. <c>0</c> is the top lane.<br/>
        /// 레이어가 아이템을 넣은 레인을 가져옵니다. <c>0</c>이 맨 위 레인입니다.
        /// </summary>
        /// <remarks>
        /// This is an output of <see cref="LaneGridLayer.Arrange"/>, updated on the frame after items change. A derived layer with its own stacking writes it there.<br/>
        /// <see cref="LaneGridLayer.Arrange"/>의 결과이며 아이템이 바뀐 다음 프레임에 갱신됩니다. 자체 쌓기 방식을 가진 파생 레이어는 거기서 이 값을 씁니다.
        /// </remarks>
        public int lane { get; set; }
    }
}
