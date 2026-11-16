#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Grids
{
    /// <summary>
    /// Turns Delete, Ctrl+C, and Ctrl+V on a <see cref="GridView"/> into events. It does not change any data by itself. Attach it to <see cref="GridView.viewport"/>.<br/>
    /// <see cref="GridView"/>에서 Delete, Ctrl+C, Ctrl+V를 이벤트로 바꿉니다. 스스로는 어떤 데이터도 바꾸지 않습니다. <see cref="GridView.viewport"/>에 붙이세요.
    /// </summary>
    /// <remarks>
    /// Ctrl is the platform action key. Keys pressed inside popups are not received, because popups are outside <see cref="GridView.viewport"/>.<br/>
    /// While <see cref="GridView.readOnly"/> is <see langword="true"/>, <see cref="deleting"/> and <see cref="pasting"/> do not occur.
    /// <br/><br/>
    /// Ctrl은 플랫폼 동작 키입니다. 팝업은 <see cref="GridView.viewport"/> 밖에 있으므로 팝업 안에서 누른 키는 받지 않습니다.<br/>
    /// <see cref="GridView.readOnly"/>가 <see langword="true"/>인 동안에는 <see cref="deleting"/>과 <see cref="pasting"/>이 발생하지 않습니다.
    /// </remarks>
    /// <param name="view">
    /// The grid to handle.<br/>
    /// 처리할 그리드입니다.
    /// </param>
    public class GridEditKeyManipulator(GridView view) : Manipulator
    {
        /// <summary>
        /// Gets the grid to handle.<br/>
        /// 처리할 그리드를 가져옵니다.
        /// </summary>
        public GridView view { get; } = view;

        /// <summary>
        /// Occurs when Delete is pressed while items are selected, with the selection.<br/>
        /// 아이템이 선택된 상태에서 Delete를 누르면 선택과 함께 발생합니다.
        /// </summary>
        public event Action<GridSelection>? deleting;

        /// <summary>
        /// Occurs when Ctrl+C is pressed while items are selected, with the selection.<br/>
        /// 아이템이 선택된 상태에서 Ctrl+C를 누르면 선택과 함께 발생합니다.
        /// </summary>
        public event Action<GridSelection>? copying;

        /// <summary>
        /// Occurs when Ctrl+V is pressed while the pointer is on the grid, with what is under the pointer. Paste at <see cref="GridPointerArgs.snappedPosition"/> in <see cref="GridPointerArgs.layer"/>.<br/>
        /// 포인터가 그리드 위에 있을 때 Ctrl+V를 누르면 포인터 아래의 것들과 함께 발생합니다. <see cref="GridPointerArgs.layer"/>의 <see cref="GridPointerArgs.snappedPosition"/>에 붙여 넣으세요.
        /// </summary>
        public event Action<GridPointerArgs>? pasting;

        /// <inheritdoc/>
        protected override void RegisterCallbacksOnTarget() => target.RegisterCallback<KeyDownEvent>(OnKeyDown);

        /// <inheritdoc/>
        protected override void UnregisterCallbacksFromTarget() => target.UnregisterCallback<KeyDownEvent>(OnKeyDown);

        void OnKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Delete when !view.readOnly && view.selection.Count > 0:
                    deleting?.Invoke(view.selection);
                    break;
                case KeyCode.C when evt.actionKey && view.selection.Count > 0:
                    copying?.Invoke(view.selection);
                    break;
                case KeyCode.V when evt.actionKey && !view.readOnly && view.pointer is { } pointer:
                    pasting?.Invoke(pointer);
                    break;
                default:
                    return;
            }

            evt.StopPropagation();
        }
    }
}
