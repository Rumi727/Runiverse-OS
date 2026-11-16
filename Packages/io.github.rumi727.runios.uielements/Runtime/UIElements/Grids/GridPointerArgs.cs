#nullable enable
using UnityEngine;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// What is under the pointer on a grid, given with pointer events of <see cref="GridSelectManipulator"/>.<br/>
    /// <see cref="GridSelectManipulator"/>의 포인터 이벤트와 함께 주어지는, 그리드에서 포인터 아래에 있는 것들입니다.
    /// </summary>
    public readonly record struct GridPointerArgs
    {
        /// <summary>
        /// Gets the mouse button: <c>0</c> left, <c>1</c> right, <c>2</c> middle, or <c>-1</c> for none.<br/>
        /// 마우스 버튼을 가져옵니다. <c>0</c>은 왼쪽, <c>1</c>은 오른쪽, <c>2</c>는 가운데이며 없으면 <c>-1</c>입니다.
        /// </summary>
        public int button { get; init; }

        /// <summary>
        /// Gets the number of quick consecutive clicks, such as <c>2</c> for a double click.<br/>
        /// 빠르게 연속된 클릭 수를 가져옵니다. 예를 들어 더블 클릭이면 <c>2</c>입니다.
        /// </summary>
        public int clickCount { get; init; }

        /// <summary>
        /// Gets the modifier keys held down.<br/>
        /// 눌려 있던 보조 키를 가져옵니다.
        /// </summary>
        public EventModifiers modifiers { get; init; }

        /// <summary>
        /// Gets whether the platform action key, Ctrl or Command, was held down.<br/>
        /// 플랫폼의 동작 키(Ctrl 또는 Command)가 눌려 있었는지 여부를 가져옵니다.
        /// </summary>
        public bool actionKey { get; init; }

        /// <summary>
        /// Gets the pointer position in panel coordinates, for example to open a menu there.<br/>
        /// 패널 좌표의 포인터 위치를 가져옵니다. 예를 들어 그 자리에 메뉴를 열 때 씁니다.
        /// </summary>
        public Vector2 panelPosition { get; init; }

        /// <summary>
        /// Gets the raw axis position under the pointer, without snapping.<br/>
        /// 포인터 아래의 축 위치를 맞춤 없이 그대로 가져옵니다.
        /// </summary>
        public double position { get; init; }

        /// <summary>
        /// Gets <see cref="position"/> snapped with <see cref="GridView.Snap"/>. It equals <see cref="position"/> while <see cref="GridView.snapBypassModifier"/> is held.<br/>
        /// <see cref="GridView.Snap"/>으로 맞춘 <see cref="position"/>을 가져옵니다. <see cref="GridView.snapBypassModifier"/>를 누르고 있으면 <see cref="position"/>과 같습니다.
        /// </summary>
        public double snappedPosition { get; init; }

        /// <summary>
        /// Gets the vertical position in <see cref="layer"/>, such as a lane or row, or <c>0</c> without a layer.<br/>
        /// <see cref="layer"/> 안의 레인이나 행 같은 세로 위치를 가져옵니다. 레이어가 없으면 <c>0</c>입니다.
        /// </summary>
        public double verticalPosition { get; init; }

        /// <summary>
        /// Gets the item under the pointer, or <see langword="null"/>.<br/>
        /// 포인터 아래의 아이템을 가져옵니다. 없으면 <see langword="null"/>입니다.
        /// </summary>
        public GridItem? item { get; init; }

        /// <summary>
        /// Gets the track layer under the pointer, or <see langword="null"/>. <see cref="GridView.overlay"/> is never reported.<br/>
        /// 포인터 아래의 트랙 레이어를 가져옵니다. 없으면 <see langword="null"/>이며, <see cref="GridView.overlay"/>는 보고하지 않습니다.
        /// </summary>
        public GridLayer? layer { get; init; }

        /// <summary>
        /// Gets the track under the pointer, including its header, or <see langword="null"/>.<br/>
        /// 헤더를 포함해 포인터 아래에 있는 트랙을 가져옵니다. 없으면 <see langword="null"/>입니다.
        /// </summary>
        public GridTrack? track { get; init; }

        /// <summary>
        /// Gets the ruler under the pointer, or <see langword="null"/>.<br/>
        /// 포인터 아래의 눈금자를 가져옵니다. 없으면 <see langword="null"/>입니다.
        /// </summary>
        public GridRuler? ruler { get; init; }
    }
}
