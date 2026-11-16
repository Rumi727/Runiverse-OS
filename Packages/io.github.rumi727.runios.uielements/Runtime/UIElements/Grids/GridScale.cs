#nullable enable
namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// Decides where grid lines and ruler labels go for the current zoom level.<br/>
    /// Derive from this class to show any unit on an axis, including non-linear ones such as seconds over a tempo-changing tick axis.
    /// <br/><br/>
    /// 현재 확대 수준에서 그리드 선과 눈금자 라벨을 어디에 둘지 정합니다.<br/>
    /// 이 클래스를 상속하면 템포가 바뀌는 틱 축 위의 초처럼 비선형인 단위도 축에 표시할 수 있습니다.
    /// </summary>
    /// <remarks>
    /// Changing a property of a scale does not redraw the grid by itself. Call <see cref="GridAxis.NotifyChanged"/> afterwards.<br/>
    /// 스케일의 속성을 바꿔도 그리드가 저절로 다시 그려지지 않습니다. 바꾼 뒤 <see cref="GridAxis.NotifyChanged"/>를 호출하세요.
    /// </remarks>
    public abstract class GridScale
    {
        /// <summary>
        /// Gets or sets whether layers draw the lines of this scale behind their items. Rulers draw the scale regardless.<br/>
        /// 레이어가 아이템 뒤에 이 스케일의 선을 그리는지 여부를 가져오거나 설정합니다. 눈금자는 이 값과 상관없이 그립니다.
        /// </summary>
        public bool drawLines { get; set; } = true;

        /// <summary>
        /// Adds the lines inside <paramref name="range"/> to <paramref name="lines"/>.<br/>
        /// <paramref name="range"/> 안에 있는 선들을 <paramref name="lines"/>에 추가합니다.
        /// </summary>
        /// <param name="range">
        /// The visible axis range.<br/>
        /// 보이는 축 범위입니다.
        /// </param>
        /// <param name="pixelsPerUnit">
        /// How many pixels one axis unit currently takes. Use it to keep lines from getting too dense.<br/>
        /// 현재 축 1단위가 차지하는 픽셀 수입니다. 선이 너무 촘촘해지지 않게 하는 데 사용합니다.
        /// </param>
        /// <param name="includeLabels">
        /// Whether <see cref="GridLine.label"/> is used. <see cref="GridRuler"/> passes <see langword="true"/>; layers pass <see langword="false"/> because they draw only the lines, so labels need not be made.<br/>
        /// <see cref="GridLine.label"/>을 쓰는지 여부입니다. <see cref="GridRuler"/>는 <see langword="true"/>를 넘기고, 레이어는 선만 그리므로 <see langword="false"/>를 넘깁니다. 이때는 라벨을 만들 필요가 없습니다.
        /// </param>
        /// <param name="lines">
        /// The list to add to. It is not cleared by this method.<br/>
        /// 추가할 목록입니다. 이 메서드는 목록을 비우지 않습니다.
        /// </param>
        public abstract void GetLines(GridRange range, double pixelsPerUnit, bool includeLabels, List<GridLine> lines);

        /// <summary>
        /// Moves <paramref name="position"/> to the nearest position this scale snaps to. The default returns it unchanged.<br/>
        /// <paramref name="position"/>을 이 스케일이 맞추는 가장 가까운 위치로 옮깁니다. 기본 구현은 그대로 반환합니다.
        /// </summary>
        /// <param name="position">
        /// The axis position to snap.<br/>
        /// 맞출 축 위치입니다.
        /// </param>
        /// <param name="pixelsPerUnit">
        /// How many pixels one axis unit currently takes.<br/>
        /// 현재 축 1단위가 차지하는 픽셀 수입니다.
        /// </param>
        /// <returns>
        /// The snapped axis position.<br/>
        /// 맞춘 축 위치를 반환합니다.
        /// </returns>
        public virtual double Snap(double position, double pixelsPerUnit) => position;
    }
}
