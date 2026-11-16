#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// Zooms and pans a <see cref="GridView"/> with the mouse wheel and dragging. Attach it to <see cref="GridView.viewport"/>.<br/>
    /// 마우스 휠과 끌기로 <see cref="GridView"/>를 확대·축소하고 이동합니다. <see cref="GridView.viewport"/>에 붙이세요.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Wheel: zooms horizontally around the pointer.</description></item>
    /// <item><description><see cref="horizontalModifier"/> + wheel, or a horizontal wheel: pans horizontally.</description></item>
    /// <item><description><see cref="verticalModifier"/> + wheel: scrolls the tracks vertically.</description></item>
    /// <item><description>Middle drag, or <see cref="dragModifier"/> + left drag: pans in both directions.</description></item>
    /// </list>
    /// <br/><br/>
    /// <list type="bullet">
    /// <item><description>휠: 포인터를 기준으로 가로 확대·축소합니다.</description></item>
    /// <item><description><see cref="horizontalModifier"/> + 휠, 또는 가로 휠: 가로로 이동합니다.</description></item>
    /// <item><description><see cref="verticalModifier"/> + 휠: 트랙을 세로로 스크롤합니다.</description></item>
    /// <item><description>가운데 끌기, 또는 <see cref="dragModifier"/> + 왼쪽 끌기: 양방향으로 이동합니다.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="view">
    /// The grid to zoom and pan.<br/>
    /// 확대·축소하고 이동할 그리드입니다.
    /// </param>
    public class GridPanZoomManipulator(GridView view) : Manipulator
    {
        int pointerId = -1;
        Vector2 lastPosition;

        /// <summary>
        /// Gets the grid to zoom and pan.<br/>
        /// 확대·축소하고 이동할 그리드를 가져옵니다.
        /// </summary>
        public GridView view { get; } = view;

        /// <summary>
        /// Gets or sets how much one wheel tick zooms.<br/>
        /// 휠 한 칸에 확대·축소하는 배율을 가져오거나 설정합니다.
        /// </summary>
        public double zoomFactor { get; set; } = 1.15;

        /// <summary>
        /// Gets or sets how many pixels one wheel tick pans or scrolls.<br/>
        /// 휠 한 칸에 이동하거나 스크롤하는 픽셀 수를 가져오거나 설정합니다.
        /// </summary>
        public float scrollDistance { get; set; } = 40;

        /// <summary>
        /// Gets or sets the modifier key that makes the wheel pan horizontally.<br/>
        /// 휠로 가로 이동하게 하는 보조 키를 가져오거나 설정합니다.
        /// </summary>
        public EventModifiers horizontalModifier { get; set; } = EventModifiers.Shift;

        /// <summary>
        /// Gets or sets the modifier key that makes the wheel scroll vertically.<br/>
        /// 휠로 세로 스크롤하게 하는 보조 키를 가져오거나 설정합니다.
        /// </summary>
        public EventModifiers verticalModifier { get; set; } = EventModifiers.Control;

        /// <summary>
        /// Gets or sets the modifier key that makes a left drag pan.<br/>
        /// 왼쪽 끌기로 이동하게 하는 보조 키를 가져오거나 설정합니다.
        /// </summary>
        public EventModifiers dragModifier { get; set; } = EventModifiers.Alt;

        /// <summary>
        /// Gets whether the manipulator is panning by dragging.<br/>
        /// 조작기가 끌기로 이동하는 중인지 여부를 가져옵니다.
        /// </summary>
        public bool isDragging => pointerId >= 0;

        /// <inheritdoc/>
        protected override void RegisterCallbacksOnTarget()
        {
            // The wheel is taken before the scroll view, which would otherwise scroll on every wheel.
            target.RegisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        /// <inheritdoc/>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<WheelEvent>(OnWheel, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);

            pointerId = -1;
        }

        void OnWheel(WheelEvent evt)
        {
            Vector3 delta = evt.delta;
            float ticks = delta.y / WheelEvent.scrollDeltaPerTick;

            if (verticalModifier != EventModifiers.None && (evt.modifiers & verticalModifier) != 0)
                view.scrollView.scrollOffset += new Vector2(0, ticks * scrollDistance);
            else if (horizontalModifier != EventModifiers.None && (evt.modifiers & horizontalModifier) != 0)
                view.axis.Pan(-(ticks + (delta.x / WheelEvent.scrollDeltaPerTick)) * scrollDistance);
            else if (delta.x != 0)
                view.axis.Pan(-(delta.x / WheelEvent.scrollDeltaPerTick) * scrollDistance);
            else
                view.axis.Zoom(Math.Pow(zoomFactor, -ticks), view.GetAxisPixel(evt.mousePosition));

            evt.StopPropagation();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (isDragging)
                return;

            bool middle = evt.button == (int)MouseButton.MiddleMouse;
            bool modifiedLeft = evt.button == (int)MouseButton.LeftMouse && dragModifier != EventModifiers.None && (evt.modifiers & dragModifier) != 0;
            if (!middle && !modifiedLeft)
                return;

            pointerId = evt.pointerId;
            lastPosition = evt.position;
            target.CapturePointer(pointerId);
            evt.StopPropagation();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != pointerId)
                return;

            Vector2 position = evt.position;
            Vector2 delta = position - lastPosition;
            lastPosition = position;

            view.axis.Pan(delta.x);
            view.scrollView.scrollOffset -= new Vector2(0, delta.y);
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != pointerId)
                return;

            End();
            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt) => End();

        void End()
        {
            if (pointerId < 0)
                return;

            int id = pointerId;
            pointerId = -1;
            if (target.HasPointerCapture(id))
                target.ReleasePointer(id);
        }
    }
}
