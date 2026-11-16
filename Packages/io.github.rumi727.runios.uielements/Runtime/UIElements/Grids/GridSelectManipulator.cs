#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// Handles left and right clicks and drags on a <see cref="GridView"/>: selecting, box selecting, and moving items. Attach it to <see cref="GridView.viewport"/>.<br/>
    /// Each step raises an event first, so the default behavior can be replaced from outside.
    /// <br/><br/>
    /// <see cref="GridView"/>에서 왼쪽·오른쪽 클릭과 끌기를 처리합니다. 선택, 상자 선택, 아이템 이동을 합니다. <see cref="GridView.viewport"/>에 붙이세요.<br/>
    /// 각 단계에서 이벤트를 먼저 발생시키므로, 기본 동작을 밖에서 바꿀 수 있습니다.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Click on an item: selects only it. With the action key: toggles it. A right click on a selected item keeps the selection.</description></item>
    /// <item><description>Click on empty space: clears the selection, unless the action key is held.</description></item>
    /// <item><description>Left drag from an item: moves the selection, snapping with <see cref="GridView.Snap"/> unless <see cref="GridView.snapBypassModifier"/> is held. Esc cancels. Blocked by <see cref="GridView.readOnly"/>.</description></item>
    /// <item><description>Left drag from empty space: box selection. With the action key, adds to the selection.</description></item>
    /// <item><description>Presses on track headers and the corner are ignored. Clicks and drags on rulers only raise events.</description></item>
    /// </list>
    /// <br/><br/>
    /// <list type="bullet">
    /// <item><description>아이템 클릭: 그 아이템만 선택합니다. 동작 키를 누르면 선택을 전환합니다. 선택된 아이템을 오른쪽 클릭하면 선택을 유지합니다.</description></item>
    /// <item><description>빈 곳 클릭: 동작 키를 누르지 않았으면 선택을 해제합니다.</description></item>
    /// <item><description>아이템에서 왼쪽 끌기: <see cref="GridView.snapBypassModifier"/>를 누르지 않았으면 <see cref="GridView.Snap"/>으로 맞추며 선택을 옮깁니다. Esc로 취소합니다. <see cref="GridView.readOnly"/>이면 막힙니다.</description></item>
    /// <item><description>빈 곳에서 왼쪽 끌기: 상자 선택입니다. 동작 키를 누르면 선택에 추가합니다.</description></item>
    /// <item><description>트랙 헤더와 모서리에서 누르면 무시합니다. 눈금자에서의 클릭과 끌기는 이벤트만 발생시킵니다.</description></item>
    /// </list>
    /// </remarks>
    /// <param name="view">
    /// The grid to handle.<br/>
    /// 처리할 그리드입니다.
    /// </param>
    public partial class GridSelectManipulator(GridView view) : Manipulator
    {
        enum State
        {
            none,
            pressed,
            ignored,
            userDrag,
            move,
            marquee
        }

        State state;
        int pointerId = -1;
        Vector2 pressPanelPosition;
        GridPointerArgs press;
        GridPointerArgs last;

        /// <summary>
        /// Gets the grid to handle.<br/>
        /// 처리할 그리드를 가져옵니다.
        /// </summary>
        public GridView view { get; } = view;

        /// <summary>
        /// Gets or sets how many pixels the pointer must move before a press becomes a drag.<br/>
        /// 누른 것이 끌기가 되려면 포인터가 몇 픽셀 움직여야 하는지 가져오거나 설정합니다.
        /// </summary>
        public float dragDistance { get; set; } = 4;

        /// <summary>
        /// Occurs when a press is released without dragging, for left and right buttons. Return <see langword="true"/> to skip the default selection behavior.<br/>
        /// The arguments describe what was under the pointer when it was pressed.
        /// <br/><br/>
        /// 끌지 않고 누른 것을 놓았을 때 왼쪽·오른쪽 버튼 모두에서 발생합니다. <see langword="true"/>를 반환하면 기본 선택 동작을 건너뜁니다.<br/>
        /// 인수는 눌렀을 때 포인터 아래에 있던 것들입니다.
        /// </summary>
        public event Func<GridPointerArgs, bool>? clicking;

        /// <summary>
        /// Occurs when a press starts to drag. Return <see langword="true"/> to take over the drag; then only <see cref="dragging"/> and <see cref="dragEnded"/> occur, and nothing is moved or selected.<br/>
        /// The arguments describe what was under the pointer when it was pressed.
        /// <br/><br/>
        /// 누른 것이 끌기를 시작할 때 발생합니다. <see langword="true"/>를 반환하면 끌기를 가져가며, 그 뒤로는 <see cref="dragging"/>과 <see cref="dragEnded"/>만 발생하고 아무것도 옮기거나 선택하지 않습니다.<br/>
        /// 인수는 눌렀을 때 포인터 아래에 있던 것들입니다.
        /// </summary>
        public event Func<GridPointerArgs, bool>? dragStarting;

        /// <summary>
        /// Occurs while a drag taken over in <see cref="dragStarting"/> moves.<br/>
        /// <see cref="dragStarting"/>에서 가져간 끌기가 움직이는 동안 발생합니다.
        /// </summary>
        public event Action<GridPointerArgs>? dragging;

        /// <summary>
        /// Occurs when a drag taken over in <see cref="dragStarting"/> ends.<br/>
        /// <see cref="dragStarting"/>에서 가져간 끌기가 끝나면 발생합니다.
        /// </summary>
        public event Action<GridPointerArgs>? dragEnded;

        /// <summary>
        /// Occurs when the selection starts to move, with the items being moved.<br/>
        /// 선택이 움직이기 시작할 때, 옮겨지는 아이템과 함께 발생합니다.
        /// </summary>
        public event Action<IReadOnlyList<GridItem>>? moveStarted;

        /// <summary>
        /// Occurs when moving ends and the items changed position. The items already have their new values.<br/>
        /// 이동이 끝나고 아이템 위치가 바뀌었으면 발생합니다. 아이템에는 이미 새 값이 들어 있습니다.
        /// </summary>
        public event Action<GridMoveResult>? moved;

        /// <summary>
        /// Occurs when moving is canceled with Esc or ends without changes. The items are back at their original positions.<br/>
        /// Esc로 이동을 취소했거나 바뀐 것 없이 끝나면 발생합니다. 아이템은 원래 위치로 돌아가 있습니다.
        /// </summary>
        public event Action? moveCanceled;

        /// <inheritdoc/>
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        /// <inheritdoc/>
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);

            Finish(false);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (state != State.none || evt.button is not ((int)MouseButton.LeftMouse or (int)MouseButton.RightMouse))
                return;

            GridPointerArgs args = view.CreatePointerArgs(evt.position, evt.button, evt.clickCount, evt.modifiers, evt.actionKey);
            if (args.ruler == null && (view.GetAxisPixel(evt.position) < 0 || (args.track != null && args.layer == null)))
                return;

            press = args;
            last = args;
            pressPanelPosition = evt.position;
            pointerId = evt.pointerId;
            state = State.pressed;

            target.CapturePointer(pointerId);
            view.viewport.Focus();
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (evt.pointerId != pointerId)
                return;

            if (state == State.pressed)
            {
                if (((Vector2)evt.position - pressPanelPosition).sqrMagnitude < dragDistance * dragDistance)
                    return;

                StartDrag();
            }

            last = view.CreatePointerArgs(evt.position, press.button, press.clickCount, evt.modifiers, evt.actionKey);
            switch (state)
            {
                case State.userDrag:
                    dragging?.Invoke(last);
                    break;
                case State.move:
                    UpdateMove(evt.position, evt.modifiers);
                    break;
                case State.marquee:
                    UpdateMarquee(evt.position);
                    break;
            }

            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId != pointerId)
                return;

            bool click = state == State.pressed;
            Finish(true);

            if (click)
                Click(press);

            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt) => Finish(true);

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Escape || state is not (State.move or State.marquee))
                return;

            Finish(false);
            evt.StopPropagation();
        }

        void StartDrag()
        {
            if (dragStarting?.Invoke(press) == true)
            {
                state = State.userDrag;
                return;
            }

            if (press.button != (int)MouseButton.LeftMouse || press.ruler != null)
            {
                state = State.ignored;
                return;
            }

            if (press.item is { } item)
            {
                if (view.readOnly)
                {
                    state = State.ignored;
                    return;
                }

                if (!item.selected)
                    view.selection.Set(item);

                BeginMove(item);
                state = State.move;
            }
            else
            {
                BeginMarquee();
                state = State.marquee;
            }
        }

        void Click(GridPointerArgs args)
        {
            if (clicking?.Invoke(args) == true || args.ruler != null)
                return;

            if (args.item is { } item)
            {
                if (args.actionKey)
                    view.selection.Toggle(item);
                else if (args.button != (int)MouseButton.RightMouse || !item.selected)
                    view.selection.Set(item);
            }
            else if (!args.actionKey)
                view.selection.Clear();
        }

        // commit is false when the gesture is canceled, which puts moved items back.
        void Finish(bool commit)
        {
            State finished = state;
            state = State.none;

            int id = pointerId;
            pointerId = -1;
            if (id >= 0 && target != null && target.HasPointerCapture(id))
                target.ReleasePointer(id);

            switch (finished)
            {
                case State.userDrag:
                    dragEnded?.Invoke(last);
                    break;
                case State.move:
                    EndMove(commit);
                    break;
                case State.marquee:
                    EndMarquee();
                    break;
            }
        }
    }
}
