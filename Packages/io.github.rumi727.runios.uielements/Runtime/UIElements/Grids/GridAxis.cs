#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// The horizontal axis of a grid: which position is at the left edge and how many pixels one unit takes.<br/>
    /// The unit is a plain <see cref="double"/> and means whatever the user wants, such as beats, ticks, or seconds.
    /// <br/><br/>
    /// 그리드의 가로축입니다. 왼쪽 끝이 어느 위치인지와 1단위가 몇 픽셀인지를 가집니다.<br/>
    /// 단위는 그냥 <see cref="double"/>이며 비트, 틱, 초 등 사용자가 정한 의미를 가집니다.
    /// </summary>
    /// <remarks>
    /// Pixel values are measured from the left edge of the item area, not including the header column.<br/>
    /// 픽셀 값은 헤더 열을 뺀 아이템 영역의 왼쪽 끝부터 잽니다.
    /// </remarks>
    public sealed class GridAxis
    {
        /// <summary>
        /// Gets or sets the axis position at the left edge of the item area.<br/>
        /// 아이템 영역 왼쪽 끝의 축 위치를 가져오거나 설정합니다.
        /// </summary>
        /// <remarks>
        /// When <see cref="limit"/> is set, the value is kept so that the view stays inside it.<br/>
        /// <see cref="limit"/>가 설정되어 있으면 보이는 범위가 그 안에 머물도록 값을 조정합니다.
        /// </remarks>
        public double offset
        {
            get => _offset;
            set => SetView(value, _pixelsPerUnit);
        }
        double _offset;

        /// <summary>
        /// Gets or sets how many pixels one axis unit takes. The value is kept between <see cref="minimumPixelsPerUnit"/> and <see cref="maximumPixelsPerUnit"/>.<br/>
        /// 축 1단위가 차지하는 픽셀 수를 가져오거나 설정합니다. 값은 <see cref="minimumPixelsPerUnit"/>과 <see cref="maximumPixelsPerUnit"/> 사이로 유지됩니다.
        /// </summary>
        public double pixelsPerUnit
        {
            get => _pixelsPerUnit;
            set => SetView(_offset, value);
        }
        double _pixelsPerUnit = 20;

        /// <summary>
        /// Gets or sets the smallest allowed <see cref="pixelsPerUnit"/>, which limits zooming out.<br/>
        /// 허용되는 가장 작은 <see cref="pixelsPerUnit"/>을 가져오거나 설정합니다. 축소 한계를 정합니다.
        /// </summary>
        public double minimumPixelsPerUnit { get; set; } = 1e-6;

        /// <summary>
        /// Gets or sets the largest allowed <see cref="pixelsPerUnit"/>, which limits zooming in.<br/>
        /// 허용되는 가장 큰 <see cref="pixelsPerUnit"/>을 가져오거나 설정합니다. 확대 한계를 정합니다.
        /// </summary>
        public double maximumPixelsPerUnit { get; set; } = 1e6;

        /// <summary>
        /// Gets or sets the range the view may show, or <see langword="null"/> for no limit.<br/>
        /// 보여 줄 수 있는 범위를 가져오거나 설정합니다. <see langword="null"/>이면 제한하지 않습니다.
        /// </summary>
        public GridRange? limit
        {
            get;
            set
            {
                field = value;
                SetView(offset, pixelsPerUnit);
            }
        }

        /// <summary>
        /// Gets or sets the width of the item area in pixels. <see cref="GridView"/> sets it from its layout.<br/>
        /// 아이템 영역의 픽셀 폭을 가져오거나 설정합니다. <see cref="GridView"/>가 레이아웃에 맞춰 설정합니다.
        /// </summary>
        public float width
        {
            get;
            set
            {
                if (field.Approximately(value))
                    return;

                field = value;
                changed?.Invoke();
            }
        }

        /// <summary>
        /// Gets the scales whose lines are drawn behind the items. The first one is used for <see cref="Snap"/> when <see cref="snap"/> is <see langword="null"/>.<br/>
        /// 아이템 뒤에 선을 그릴 스케일 목록을 가져옵니다. <see cref="snap"/>이 <see langword="null"/>이면 첫 스케일로 <see cref="Snap"/>합니다.
        /// </summary>
        /// <remarks>
        /// Call <see cref="NotifyChanged"/> after changing the list.<br/>
        /// 목록을 바꾼 뒤에는 <see cref="NotifyChanged"/>를 호출하세요.
        /// </remarks>
        public List<GridScale> scales { get; } = [];

        /// <summary>
        /// Gets or sets a function that replaces the default snapping of <see cref="Snap"/>.<br/>
        /// <see cref="Snap"/>의 기본 맞춤 동작을 대신할 함수를 가져오거나 설정합니다.
        /// </summary>
        public Func<double, double>? snap { get; set; }

        /// <summary>
        /// Occurs when <see cref="offset"/>, <see cref="pixelsPerUnit"/>, <see cref="width"/>, or anything drawn from the axis changes.<br/>
        /// <see cref="offset"/>, <see cref="pixelsPerUnit"/>, <see cref="width"/> 또는 축에서 그려지는 무언가가 바뀌면 발생합니다.
        /// </summary>
        public event Action? changed;

        /// <summary>
        /// Converts an axis position to a pixel in the item area.<br/>
        /// 축 위치를 아이템 영역의 픽셀로 변환합니다.
        /// </summary>
        /// <param name="position">
        /// The axis position.<br/>
        /// 축 위치입니다.
        /// </param>
        /// <returns>
        /// The pixel measured from the left edge of the item area.<br/>
        /// 아이템 영역 왼쪽 끝부터 잰 픽셀을 반환합니다.
        /// </returns>
        public float ToPixel(double position) => (float)((position - offset) * pixelsPerUnit);

        /// <summary>
        /// Converts a pixel in the item area to an axis position.<br/>
        /// 아이템 영역의 픽셀을 축 위치로 변환합니다.
        /// </summary>
        /// <param name="pixel">
        /// The pixel measured from the left edge of the item area.<br/>
        /// 아이템 영역 왼쪽 끝부터 잰 픽셀입니다.
        /// </param>
        /// <returns>
        /// The axis position at that pixel.<br/>
        /// 그 픽셀의 축 위치를 반환합니다.
        /// </returns>
        public double ToPosition(float pixel) => offset + (pixel / pixelsPerUnit);

        /// <summary>
        /// Gets the axis range shown in the item area.<br/>
        /// 아이템 영역에 보이는 축 범위를 가져옵니다.
        /// </summary>
        /// <returns>
        /// The visible range.<br/>
        /// 보이는 범위를 반환합니다.
        /// </returns>
        public GridRange GetRange() => new(offset, offset + (width / pixelsPerUnit));

        /// <summary>
        /// Snaps <paramref name="position"/> with <see cref="snap"/>, or with the first of <see cref="scales"/> if it is <see langword="null"/>.<br/>
        /// <paramref name="position"/>을 <see cref="snap"/>으로 맞추며, 그것이 <see langword="null"/>이면 <see cref="scales"/>의 첫 스케일로 맞춥니다.
        /// </summary>
        /// <param name="position">
        /// The axis position to snap.<br/>
        /// 맞출 축 위치입니다.
        /// </param>
        /// <returns>
        /// The snapped position, or <paramref name="position"/> unchanged when there is nothing to snap to.<br/>
        /// 맞춘 위치를 반환하며, 맞출 기준이 없으면 <paramref name="position"/>을 그대로 반환합니다.
        /// </returns>
        public double Snap(double position)
        {
            if (snap != null)
                return snap(position);

            return scales.Count > 0 ? scales[0].Snap(position, pixelsPerUnit) : position;
        }

        /// <summary>
        /// Sets <see cref="offset"/> and <see cref="pixelsPerUnit"/> together and raises <see cref="changed"/> once.<br/>
        /// <see cref="offset"/>과 <see cref="pixelsPerUnit"/>을 함께 설정하고 <see cref="changed"/>를 한 번만 발생시킵니다.
        /// </summary>
        /// <param name="offset">
        /// The new axis position at the left edge.<br/>
        /// 왼쪽 끝의 새 축 위치입니다.
        /// </param>
        /// <param name="pixelsPerUnit">
        /// The new pixels per unit.<br/>
        /// 새 단위당 픽셀 수입니다.
        /// </param>
        public void SetView(double offset, double pixelsPerUnit)
        {
            pixelsPerUnit = Math.Clamp(pixelsPerUnit, minimumPixelsPerUnit, maximumPixelsPerUnit);

            if (limit is { } range)
            {
                double visibleLength = width / pixelsPerUnit;
                offset = visibleLength >= range.length ? range.start : Math.Clamp(offset, range.start, range.end - visibleLength);
            }

            if (_offset.Approximately(offset) && _pixelsPerUnit.Approximately(pixelsPerUnit))
                return;

            _offset = offset;
            _pixelsPerUnit = pixelsPerUnit;
            changed?.Invoke();
        }

        /// <summary>
        /// Multiplies <see cref="pixelsPerUnit"/> by <paramref name="factor"/> while keeping the position under <paramref name="anchorPixel"/> in place.<br/>
        /// <paramref name="anchorPixel"/> 아래의 위치를 고정한 채 <see cref="pixelsPerUnit"/>에 <paramref name="factor"/>를 곱합니다.
        /// </summary>
        /// <param name="factor">
        /// The zoom factor. Values above <c>1</c> zoom in.<br/>
        /// 확대 배율입니다. <c>1</c>보다 크면 확대합니다.
        /// </param>
        /// <param name="anchorPixel">
        /// The pixel in the item area that stays on the same position, usually the pointer.<br/>
        /// 같은 위치에 머무는 아이템 영역의 픽셀이며, 보통 포인터 위치입니다.
        /// </param>
        public void Zoom(double factor, float anchorPixel)
        {
            double anchor = ToPosition(anchorPixel);
            double newPixelsPerUnit = Math.Clamp(pixelsPerUnit * factor, minimumPixelsPerUnit, maximumPixelsPerUnit);
            SetView(anchor - (anchorPixel / newPixelsPerUnit), newPixelsPerUnit);
        }

        /// <summary>
        /// Moves the contents by <paramref name="pixels"/>. Positive values move them to the right, showing earlier positions.<br/>
        /// 내용을 <paramref name="pixels"/>만큼 옮깁니다. 양수면 오른쪽으로 옮겨 앞쪽 위치를 보여 줍니다.
        /// </summary>
        /// <param name="pixels">
        /// The distance to move in pixels.<br/>
        /// 옮길 픽셀 거리입니다.
        /// </param>
        public void Pan(float pixels) => SetView(offset - (pixels / pixelsPerUnit), pixelsPerUnit);

        /// <summary>
        /// Shows exactly <paramref name="range"/> across the current <see cref="width"/>.<br/>
        /// 현재 <see cref="width"/>에 <paramref name="range"/>가 꼭 맞게 보이도록 합니다.
        /// </summary>
        /// <param name="range">
        /// The range to show.<br/>
        /// 보여 줄 범위입니다.
        /// </param>
        public void Frame(GridRange range) => SetView(range.start, width / range.length);

        /// <summary>
        /// Raises <see cref="changed"/> so that everything drawn from the axis is updated, for example after editing <see cref="scales"/>.<br/>
        /// <see cref="changed"/>를 발생시켜 축에서 그려지는 모든 것을 갱신합니다. 예를 들어 <see cref="scales"/>를 바꾼 뒤에 호출합니다.
        /// </summary>
        public void NotifyChanged() => changed?.Invoke();
    }
}
