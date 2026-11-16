#nullable enable
using UnityEngine;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A one-dimensional layer. Items whose drawn rectangles overlap are moved down into new lanes, and the layer grows to fit them.<br/>
    /// 1차원 레이어입니다. 그려진 사각형이 겹치는 아이템은 아래 레인으로 옮겨지고, 레이어는 그에 맞게 높아집니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Items are placed in the order of <see cref="GridLayer{TItem}.items"/>: each goes into the top-most lane where it overlaps no earlier item.<br/>
    /// So an earlier item is always above a later one it overlaps, even when they start at the same position.<br/>
    /// Overlap is decided by the pixel widths of <see cref="GridLayer.GetHorizontalPixels"/>, so the lanes change with the zoom. Items that only touch do not overlap.<br/>
    /// The grid places elements at the top of their lane but does not set their height.
    /// </para>
    /// <br/><br/>
    /// <para>
    /// 아이템은 <see cref="GridLayer{TItem}.items"/> 순서대로 배치되며, 각 아이템은 앞선 아이템과 겹치지 않는 가장 위 레인에 들어갑니다.<br/>
    /// 그래서 시작 위치가 같더라도 앞선 아이템이 항상 겹치는 뒤 아이템보다 위에 있습니다.<br/>
    /// 겹침은 <see cref="GridLayer.GetHorizontalPixels"/>의 픽셀 폭으로 판단하므로, 확대 수준에 따라 레인이 바뀝니다. 맞닿기만 한 아이템은 겹치지 않습니다.<br/>
    /// 그리드는 요소를 레인 위쪽에 두지만 높이는 정하지 않습니다.
    /// </para>
    /// </remarks>
    public class LaneGridLayer : GridLayer<LaneGridItem>
    {
        /// <summary>
        /// The USS class name of lane grid layers.<br/>
        /// 레인 그리드 레이어의 USS 클래스 이름입니다.
        /// </summary>
        public new const string ussClassName = "runios-lane-grid-layer";

        // The spans placed in each lane, sorted by start. Spans in one lane never overlap, so they are also sorted by end.
        readonly List<List<(double start, double end)>> lanes = [];
        int usedLaneCount;

        /// <summary>
        /// Initializes the layer.<br/>
        /// 레이어를 초기화합니다.
        /// </summary>
        public LaneGridLayer() => AddToClassList(ussClassName);

        /// <summary>
        /// Gets or sets the height of one lane in pixels. Element heights are left to the elements.<br/>
        /// 레인 하나의 픽셀 높이를 가져오거나 설정합니다. 요소 높이는 요소에 맡깁니다.
        /// </summary>
        public float laneHeight
        {
            get;
            set
            {
                field = value;
                MarkDirty();
            }
        } = 20;

        /// <summary>
        /// Gets the number of lanes in use, as of the last refresh.<br/>
        /// 마지막 갱신 시점에 쓰이는 레인 수를 가져옵니다.
        /// </summary>
        public int laneCount => usedLaneCount;

        /// <inheritdoc/>
        protected override bool arrangesWithZoom => true;

        /// <inheritdoc/>
        protected override void Arrange()
        {
            for (int i = 0; i < usedLaneCount; i++)
                lanes[i].Clear();

            usedLaneCount = 0;
            double pixelsPerUnit = view?.axis.pixelsPerUnit ?? 1;

            foreach (LaneGridItem item in items)
            {
                // Spans are kept in axis units so that panning, which shifts every pixel, does not lose precision.
                double start = item.start;
                double end = start + (GetHorizontalPixels(item).width / pixelsPerUnit);

                int lane = 0;
                int index = 0;
                for (; lane < usedLaneCount; lane++)
                {
                    if (TryFindGap(lanes[lane], start, end, out index))
                        break;
                }

                if (lane == usedLaneCount)
                {
                    if (lane == lanes.Count)
                        lanes.Add([]);

                    usedLaneCount++;
                    index = 0;
                }

                lanes[lane].Insert(index, (start, end));
                item.lane = lane;
            }

            style.height = Math.Max(1, usedLaneCount) * laneHeight;
        }

        // Finds where [start, end) goes in a lane without overlapping its spans.
        static bool TryFindGap(List<(double start, double end)> spans, double start, double end, out int index)
        {
            int low = 0;
            int high = spans.Count;
            while (low < high)
            {
                int middle = (low + high) >>> 1;
                if (spans[middle].start < start)
                    low = middle + 1;
                else
                    high = middle;
            }

            index = low;
            if (index > 0 && spans[index - 1].end > start)
                return false;

            return index >= spans.Count || spans[index].start >= end;
        }

        /// <inheritdoc/>
        public override Rect GetItemRect(GridItem item)
        {
            (float x, float width) = GetHorizontalPixels(item);
            return new Rect(x, ((LaneGridItem)item).lane * laneHeight, width, GetItemHeight(item, laneHeight));
        }

        /// <summary>
        /// Converts a vertical layer pixel to a lane. The integer part is the lane index.<br/>
        /// 레이어의 세로 픽셀을 레인으로 변환합니다. 정수 부분이 레인 인덱스입니다.
        /// </summary>
        /// <inheritdoc/>
        public override double GetVerticalPosition(float localY) => localY / laneHeight;
    }
}
