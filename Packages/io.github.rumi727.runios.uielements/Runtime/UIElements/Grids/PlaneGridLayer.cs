#nullable enable
using UnityEngine;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A two-dimensional layer. Each item also has a vertical position, <see cref="PlaneGridItem.top"/>, in vertical units such as rows.<br/>
    /// The grid places elements there but does not set their height.
    /// <br/><br/>
    /// 2차원 레이어입니다. 각 아이템은 행 같은 세로 단위로 된 세로 위치 <see cref="PlaneGridItem.top"/>도 가집니다.<br/>
    /// 그리드는 요소를 그 위치에 두지만 높이는 정하지 않습니다.
    /// </summary>
    public class PlaneGridLayer : GridLayer<PlaneGridItem>
    {
        /// <summary>
        /// The USS class name of plane grid layers.<br/>
        /// 평면 그리드 레이어의 USS 클래스 이름입니다.
        /// </summary>
        public new const string ussClassName = "runios-plane-grid-layer";

        /// <summary>
        /// Initializes the layer.<br/>
        /// 레이어를 초기화합니다.
        /// </summary>
        public PlaneGridLayer() => AddToClassList(ussClassName);

        /// <summary>
        /// Gets or sets how many pixels one vertical unit takes.<br/>
        /// 세로 1단위가 차지하는 픽셀 수를 가져오거나 설정합니다.
        /// </summary>
        public float unitHeight
        {
            get;
            set
            {
                field = value;
                MarkDirty();
            }
        } = 20;

        /// <summary>
        /// Gets or sets the number of vertical units the layer shows, such as the number of rows. The layer is <c>verticalLength * unitHeight</c> pixels high.<br/>
        /// 레이어가 보여 주는 세로 단위 수(예: 행 수)를 가져오거나 설정합니다. 레이어 높이는 <c>verticalLength * unitHeight</c> 픽셀입니다.
        /// </summary>
        public double verticalLength
        {
            get;
            set
            {
                field = value;
                MarkDirty();
            }
        } = 1;

        /// <summary>
        /// Gets or sets the vertical unit that dragged items snap to, or <c>0</c> for no snapping.<br/>
        /// 끌린 아이템이 맞춰지는 세로 단위를 가져오거나 설정합니다. <c>0</c>이면 맞추지 않습니다.
        /// </summary>
        public double verticalSnap { get; set; } = 1;

        /// <inheritdoc/>
        protected override void Arrange() => style.height = (float)(verticalLength * unitHeight);

        /// <inheritdoc/>
        public override Rect GetItemRect(GridItem item)
        {
            PlaneGridItem planeItem = (PlaneGridItem)item;
            (float x, float width) = GetHorizontalPixels(item);
            return new Rect(x, (float)(planeItem.top * unitHeight), width, GetItemHeight(item, unitHeight));
        }

        /// <inheritdoc/>
        public override double GetVerticalPosition(float localY) => localY / unitHeight;

        /// <summary>
        /// Rounds <paramref name="position"/> to a multiple of <see cref="verticalSnap"/>.<br/>
        /// <paramref name="position"/>을 <see cref="verticalSnap"/>의 배수로 반올림합니다.
        /// </summary>
        /// <inheritdoc/>
        public override double SnapVertical(double position) => verticalSnap > 0 ? Math.Round(position / verticalSnap) * verticalSnap : position;
    }
}
