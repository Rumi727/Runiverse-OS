#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// A two-dimensional layer. Each item also has a vertical position, <see cref="PlaneGridItem.top"/>, in vertical units such as rows.<br/>
    /// The grid places elements there but does not set their height. Each whole vertical unit is a row, which can have a header and is separated from the next by a line.
    /// <br/><br/>
    /// 2차원 레이어입니다. 각 아이템은 행 같은 세로 단위로 된 세로 위치 <see cref="PlaneGridItem.top"/>도 가집니다.<br/>
    /// 그리드는 요소를 그 위치에 두지만 높이는 정하지 않습니다. 세로 1단위마다 행 하나이며, 행은 헤더를 가질 수 있고 다음 행과 선으로 구분됩니다.
    /// </summary>
    public class PlaneGridLayer : GridLayer<PlaneGridItem>
    {
        /// <summary>
        /// The USS class name of plane grid layers.<br/>
        /// 평면 그리드 레이어의 USS 클래스 이름입니다.
        /// </summary>
        public new const string ussClassName = "runios-plane-grid-layer";

        /// <summary>
        /// The USS class name of the row headers.<br/>
        /// 행 헤더의 USS 클래스 이름입니다.
        /// </summary>
        public const string rowHeaderUssClassName = ussClassName + "__row-header";

        static readonly CustomStyleProperty<Color> rowLineColorProperty = new("--runios-grid-row-line-color");

        // Below this many pixels per row, the lines between rows are not drawn, so that they do not cover the rows.
        const float minimumRowLineSpacing = 4;

        readonly List<VisualElement> _rowHeaders = [];
        Color rowLineColor = Color.clear;

        /// <summary>
        /// Initializes the layer.<br/>
        /// 레이어를 초기화합니다.
        /// </summary>
        public PlaneGridLayer()
        {
            AddToClassList(ussClassName);
            RegisterCallback<CustomStyleResolvedEvent>(OnRowStyleResolved);
        }

        /// <summary>
        /// Gets or sets how many pixels one vertical unit takes while <see cref="GridView.verticalScale"/> is <c>1</c>. One unit is drawn <c>unitHeight * verticalScale</c> pixels high.<br/>
        /// <see cref="GridView.verticalScale"/>이 <c>1</c>일 때 세로 1단위가 차지하는 픽셀 수를 가져오거나 설정합니다. 1단위는 <c>unitHeight * verticalScale</c> 픽셀 높이로 그려집니다.
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
        /// Gets or sets the number of vertical units the layer shows, such as the number of rows. The layer is <c>verticalLength * unitHeight * verticalScale</c> pixels high.<br/>
        /// 레이어가 보여 주는 세로 단위 수(예: 행 수)를 가져오거나 설정합니다. 레이어 높이는 <c>verticalLength * unitHeight * verticalScale</c> 픽셀입니다.
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

        /// <summary>
        /// Gets or sets the function that makes the header of a row from its index, or <see langword="null"/> for no row headers.<br/>
        /// The headers are put in the <see cref="GridTrack.header"/> of the track that shows the layer, at the height of their rows. Setting it removes the headers made before.
        /// <br/><br/>
        /// 행 인덱스로 행 헤더를 만드는 함수를 가져오거나 설정하며, 행 헤더가 없으면 <see langword="null"/>입니다.<br/>
        /// 헤더는 레이어를 보여 주는 트랙의 <see cref="GridTrack.header"/>에 각 행의 높이로 놓입니다. 설정하면 전에 만든 헤더를 제거합니다.
        /// </summary>
        public Func<int, VisualElement>? makeRowHeader
        {
            get;
            set
            {
                for (int i = 0; i < _rowHeaders.Count; i++)
                    _rowHeaders[i].RemoveFromHierarchy();

                _rowHeaders.Clear();
                field = value;
                MarkDirty();
            }
        }

        /// <summary>
        /// Gets the row headers made by <see cref="makeRowHeader"/>, from the top row, as of the last refresh.<br/>
        /// 마지막 갱신 시점에 <see cref="makeRowHeader"/>로 만든 행 헤더를 맨 위 행부터 가져옵니다.
        /// </summary>
        public IReadOnlyList<VisualElement> rowHeaders => _rowHeaders;

        // One vertical unit in pixels at the current vertical scale.
        float scaledUnitHeight => (float)(unitHeight * verticalScale);

        /// <inheritdoc/>
        protected override bool arrangesWithVerticalScale => true;

        /// <inheritdoc/>
        protected override void Arrange()
        {
            style.height = (float)(verticalLength * unitHeight * verticalScale);
            ArrangeRowHeaders();
        }

        void ArrangeRowHeaders()
        {
            if (makeRowHeader == null || hierarchy.parent is not GridTrack track)
                return;

            int count = (int)Math.Ceiling(verticalLength);
            while (_rowHeaders.Count > count)
            {
                _rowHeaders[^1].RemoveFromHierarchy();
                _rowHeaders.RemoveAt(_rowHeaders.Count - 1);
            }

            while (_rowHeaders.Count < count)
            {
                VisualElement header = makeRowHeader(_rowHeaders.Count);
                header.AddToClassList(rowHeaderUssClassName);
                track.header.Add(header);
                _rowHeaders.Add(header);
            }

            // The same top as the items of the row, so that a header stays beside its row at any vertical scale.
            float height = scaledUnitHeight;
            for (int i = 0; i < _rowHeaders.Count; i++)
            {
                IStyle headerStyle = _rowHeaders[i].style;
                headerStyle.top = i * height;
                headerStyle.height = height;
            }
        }

        void OnRowStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(rowLineColorProperty, out Color color))
            {
                rowLineColor = color;
                MarkDirtyRepaint();
            }
        }

        // The line of a row is its bottom pixel. The last row has none, as the bottom of the layer is the edge of the track.
        private protected override void AddRowLines(List<(Rect rect, Color color)> rects, float top, float bottom)
        {
            float height = scaledUnitHeight;
            if (rowLineColor.a <= 0 || !(height >= minimumRowLineSpacing))
                return;

            int end = Math.Min((int)Math.Ceiling(verticalLength) - 1, (int)(bottom / height) + 1);
            float width = layout.width;
            for (int row = Math.Max(0, (int)(top / height)); row < end; row++)
                rects.Add((new Rect(0, MathF.Round((row + 1) * height) - 1, width, 1), rowLineColor));
        }

        /// <inheritdoc/>
        public override Rect GetItemRect(GridItem item)
        {
            PlaneGridItem planeItem = (PlaneGridItem)item;
            (float x, float width) = GetHorizontalPixels(item);
            float height = scaledUnitHeight;
            return new Rect(x, (float)(planeItem.top * height), width, GetItemHeight(item, height));
        }

        /// <inheritdoc/>
        public override double GetVerticalPosition(float localY) => localY / scaledUnitHeight;

        /// <summary>
        /// Rounds <paramref name="position"/> to a multiple of <see cref="verticalSnap"/>.<br/>
        /// <paramref name="position"/>을 <see cref="verticalSnap"/>의 배수로 반올림합니다.
        /// </summary>
        /// <inheritdoc/>
        public override double SnapVertical(double position) => verticalSnap > 0 ? Math.Round(position / verticalSnap) * verticalSnap : position;
    }
}
