#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// An item of a <see cref="PlaneGridLayer"/> that also has a vertical position. Its height is left to its element.<br/>
    /// 세로 위치도 가지는 <see cref="PlaneGridLayer"/>의 아이템입니다. 높이는 요소에 맡깁니다.
    /// </summary>
    public class PlaneGridItem : GridItem
    {
        /// <summary>
        /// Gets or sets the vertical position of the top edge, in vertical units such as rows.<br/>
        /// 위쪽 끝의 세로 위치를 행 같은 세로 단위로 가져오거나 설정합니다.
        /// </summary>
        public double top
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                layer?.MarkDirty();
            }
        }
    }
}
