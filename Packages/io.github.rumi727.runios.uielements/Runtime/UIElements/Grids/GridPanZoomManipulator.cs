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
    /// <item><description>Wheel: zooms horizontally around the pointer. When <see cref="zoomModifier"/> is set, <see cref="zoomModifier"/> + wheel zooms and the wheel alone scrolls the tracks vertically.</description></item>
    /// <item><description><see cref="verticalZoomModifier"/> + wheel: zooms the tracks vertically around the pointer with <see cref="GridView.ZoomVertical"/>. It is off by default.</description></item>
    /// <item><description><see cref="horizontalModifier"/> + wheel, or a horizontal wheel: pans horizontally.</description></item>
    /// <item><description><see cref="verticalModifier"/> + wheel: scrolls the tracks vertically.</description></item>
    /// <item><description>Middle drag, or <see cref="dragModifier"/> + left drag: pans in both directions.</description></item>
    /// </list>
    /// <para>
    /// A wheel modifier matches only when all of its keys are held, and <see cref="EventModifiers.None"/> turns it off.<br/>
    /// When several match, the one with the most keys wins, so <c>Control | Shift</c> wins over <c>Control</c> while both are held. The order of matches with as many keys is not specified.
    /// </para>
    /// <br/><br/>
    /// <list type="bullet">
    /// <item><description>휠: 포인터를 기준으로 가로 확대·축소합니다. <see cref="zoomModifier"/>가 설정되어 있으면 <see cref="zoomModifier"/> + 휠이 확대·축소하고, 휠만 쓰면 트랙을 세로로 스크롤합니다.</description></item>
    /// <item><description><see cref="verticalZoomModifier"/> + 휠: <see cref="GridView.ZoomVertical"/>로 포인터를 기준으로 트랙을 세로 확대·축소합니다. 기본값은 꺼져 있습니다.</description></item>
    /// <item><description><see cref="horizontalModifier"/> + 휠, 또는 가로 휠: 가로로 이동합니다.</description></item>
    /// <item><description><see cref="verticalModifier"/> + 휠: 트랙을 세로로 스크롤합니다.</description></item>
    /// <item><description>가운데 끌기, 또는 <see cref="dragModifier"/> + 왼쪽 끌기: 양방향으로 이동합니다.</description></item>
    /// </list>
    /// <para>
    /// 휠 보조 키는 그 키가 모두 눌려 있을 때만 일치하며, <see cref="EventModifiers.None"/>이면 꺼집니다.<br/>
    /// 여러 개가 일치하면 키가 가장 많은 것이 우선하므로, 둘 다 눌려 있으면 <c>Control | Shift</c>가 <c>Control</c>보다 우선합니다. 키 개수가 같은 것끼리의 순서는 정해져 있지 않습니다.
    /// </para>
    /// </remarks>
    /// <param name="view">
    /// The grid to zoom and pan.<br/>
    /// 확대·축소하고 이동할 그리드입니다.
    /// </param>
    public class GridPanZoomManipulator(GridView view) : Manipulator
    {
        int pointerId = -1;

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
        /// Gets or sets the modifier key that makes the wheel zoom. With <see cref="EventModifiers.None"/>, the wheel alone zooms; otherwise the wheel alone scrolls the tracks vertically.<br/>
        /// 휠로 확대·축소하게 하는 보조 키를 가져오거나 설정합니다. <see cref="EventModifiers.None"/>이면 휠만으로 확대·축소하고, 그렇지 않으면 휠만 쓸 때 트랙을 세로로 스크롤합니다.
        /// </summary>
        public EventModifiers zoomModifier { get; set; } = EventModifiers.None;

        /// <summary>
        /// Gets or sets the modifier keys that make the wheel zoom the tracks vertically. The default, <see cref="EventModifiers.None"/>, turns vertical zooming off.<br/>
        /// 휠로 트랙을 세로 확대·축소하게 하는 보조 키를 가져오거나 설정합니다. 기본값인 <see cref="EventModifiers.None"/>이면 세로 확대·축소를 끕니다.
        /// </summary>
        public EventModifiers verticalZoomModifier { get; set; } = EventModifiers.None;

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

        const int noAction = 0;
        const int zoomAction = 1;
        const int verticalZoomAction = 2;
        const int verticalAction = 3;
        const int horizontalAction = 4;

        void OnWheel(WheelEvent evt)
        {
            Vector3 delta = evt.delta;
            float ticks = delta.y / WheelEvent.scrollDeltaPerTick;

            // This runs on every wheel tick, so the best match is tracked in locals instead of a collection.
            EventModifiers modifiers = evt.modifiers;
            int action = noAction;
            int keyCount = 0;
            if (Matches(modifiers, zoomModifier, ref keyCount))
                action = zoomAction;
            if (Matches(modifiers, verticalZoomModifier, ref keyCount))
                action = verticalZoomAction;
            if (Matches(modifiers, verticalModifier, ref keyCount))
                action = verticalAction;
            if (Matches(modifiers, horizontalModifier, ref keyCount))
                action = horizontalAction;

            if (action == zoomAction)
                view.axis.Zoom(Math.Pow(zoomFactor, -ticks), view.GetAxisPixel(evt.mousePosition));
            else if (action == verticalZoomAction)
            {
                // Shift can turn the wheel into a horizontal wheel on some platforms, so both directions count, as they do for panning.
                float verticalZoomTicks = ticks + (delta.x / WheelEvent.scrollDeltaPerTick);
                view.ZoomVertical(Math.Pow(zoomFactor, -verticalZoomTicks), view.scrollView.contentViewport.WorldToLocal(evt.mousePosition).y);
            }
            else if (action == verticalAction)
                view.scrollView.scrollOffset += new Vector2(0, ticks * scrollDistance);
            else if (action == horizontalAction)
                view.axis.Pan(-(ticks + (delta.x / WheelEvent.scrollDeltaPerTick)) * scrollDistance);
            else if (delta.x != 0)
                view.axis.Pan(-(delta.x / WheelEvent.scrollDeltaPerTick) * scrollDistance);
            else if (zoomModifier != EventModifiers.None)
                view.scrollView.scrollOffset += new Vector2(0, ticks * scrollDistance);
            else
                view.axis.Zoom(Math.Pow(zoomFactor, -ticks), view.GetAxisPixel(evt.mousePosition));

            evt.StopPropagation();
        }

        // Whether every key of candidate is held and it has more keys than the best match so far, which it then becomes.
        static bool Matches(EventModifiers modifiers, EventModifiers candidate, ref int bestKeyCount)
        {
            if (candidate == EventModifiers.None || (modifiers & candidate) != candidate)
                return false;

            int count = System.Numerics.BitOperations.PopCount((uint)candidate);
            if (count <= bestKeyCount)
                return false;

            bestKeyCount = count;
            return true;
        }

        Vector2 lastPosition;

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
