#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// 대상 요소를 누른 채 끌면 IO 항목을 끌고, 놓으면 포인터 아래 요소부터 조상으로 올라가며 찾은 <see cref="IIODropTarget"/>에 놓습니다.
    /// 끄는 동안 포인터를 잡으므로, 같은 패널 안에서만 놓을 수 있습니다. Esc를 누르면 취소합니다.
    /// </summary>
    /// <remarks>
    /// Shift를 누르고 놓으면 이동하고, 동작 키(Ctrl·Command)를 누르고 놓으면 복사하고, 둘을 함께 누르고 놓으면 링크하며, 보조 키가 없으면 놓은 자리에서 메뉴로 묻습니다.
    /// 이동과 링크는 원본과 대상이 같은 프로바이더 인스턴스일 때만 할 수 있고, 다르면 복사만 할 수 있습니다.
    /// </remarks>
    /// <param name="source">끌 항목을 정하고 고스트를 만드는 요소입니다. 보통 이 조작기를 붙이는 요소 자신입니다.</param>
    public sealed class IODragManipulator(IIODragSource source) : Manipulator
    {
        // 클릭하다 조금 움직인 것을 드래그로 보지 않도록, 이만큼 움직여야 시작합니다.
        const float startDistance = 8;

        // 놓을 때 누르고 있으면 메뉴 없이 바로 그 동작을 하는 보조 키입니다.
        static readonly KeyShortcut moveShortcut = new(KeyCode.None, modifiers: EventModifiers.Shift);
        static readonly KeyShortcut copyShortcut = new(KeyCode.None, actionKey: true);
        static readonly KeyShortcut linkShortcut = new(KeyCode.None, actionKey: true, modifiers: EventModifiers.Shift);
        static readonly KeyShortcut cancelShortcut = new(KeyCode.Escape);

        // element는 target인 요소이며, 놓을 때 메뉴를 그 요소에 엽니다. operation이 null이면 놓을 때 메뉴로 묻습니다.
        readonly record struct DropLocation(IIODropTarget target, VisualElement element, IONode directory, IODropOperation? operation);

        IODragData? data;
        int pointerId;
        Vector2 startPosition;
        Vector2 lastPosition;

        // 끄는 동안에만 있습니다. root는 고스트를 붙인 패널 맨 위 요소입니다.
        VisualElement? ghost;
        VisualElement? root;
        IIODropTarget? hoveredTarget;

        bool dragging => ghost != null;

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<PointerDownEvent>(OnPointerDown);
            target.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            target.RegisterCallback<PointerUpEvent>(OnPointerUp);
            target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<KeyUpEvent>(OnKeyUp);
            target.RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp);
            target.UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);

            End();
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (dragging || evt.button != (int)MouseButton.LeftMouse)
                return;

            data = source.GetDragData(evt);
            pointerId = evt.pointerId;
            startPosition = evt.position;
        }

        void OnPointerMove(PointerMoveEvent evt)
        {
            if (data == null || evt.pointerId != pointerId)
                return;

            // 창 밖에서 버튼을 떼는 것처럼 뗀 것을 받지 못했으면, 놓지 않고 끝냅니다.
            if ((evt.pressedButtons & 1) == 0)
            {
                End();
                return;
            }

            if (!dragging)
            {
                if (((Vector2)evt.position - startPosition).sqrMagnitude < startDistance * startDistance)
                    return;

                StartDrag(data);
            }

            UpdateDrag(evt.position, evt.actionKey, evt.shiftKey);
            evt.StopPropagation();
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (data == null || evt.pointerId != pointerId)
                return;

            if (!dragging)
            {
                End();
                return;
            }

            // 놓은 뒤 메뉴를 열거나 작업을 시작하므로, 드래그를 먼저 끝냅니다. 놓을 곳을 찾다 예외가 나도 고스트가 남지 않게 합니다.
            IODragData droppedData = data;
            DropLocation? location;
            try
            {
                location = FindDropLocation(droppedData, evt.position, evt.actionKey, evt.shiftKey, out _);
            }
            finally
            {
                End();
            }

            if (location is { } dropLocation)
            {
                if (dropLocation.operation is { } operation)
                    Drop(droppedData, dropLocation, operation).Forget();
                else
                    ShowMenu(evt.position, droppedData, dropLocation);
            }

            evt.StopPropagation();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            if (dragging)
                End();
        }

        // 끄던 요소가 패널에서 떨어지면 더는 포인터 이벤트를 받지 못하므로 끝냅니다.
        void OnDetachFromPanel(DetachFromPanelEvent evt) => End();

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!dragging)
                return;

            if (evt.keyCode == KeyCode.Escape)
                End();
            else
                UpdateDrag(lastPosition, evt.actionKey, evt.shiftKey);

            evt.StopPropagation();
        }

        // 포인터를 움직이지 않고 보조 키만 바꿔도 동작 표시가 바뀌도록 합니다.
        void OnKeyUp(KeyUpEvent evt)
        {
            if (dragging)
                UpdateDrag(lastPosition, evt.actionKey, evt.shiftKey);
        }

        void StartDrag(IODragData data)
        {
            target.CapturePointer(pointerId);

            ghost = source.OnDragStart(data);
            ghost.style.position = Position.Absolute;
            ghost.pickingMode = PickingMode.Ignore;
            ghost.Query().ForEach(static x => x.pickingMode = PickingMode.Ignore);

            // 포인터 캡처가 해제 이벤트 없이 풀려도 놓거나 움직인 것을 받도록, 패널 맨 위에서도 듣습니다.
            // 캡처가 있으면 이 요소의 처리기가 전파를 멈추므로 두 번 처리되지 않습니다.
            root = target.panel.visualTree;
            root.Add(ghost);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp);
        }

        void UpdateDrag(Vector2 position, bool actionKey, bool shiftKey)
        {
            if (data == null || ghost == null)
                return;

            lastPosition = position;
            ghost.style.left = position.x;
            ghost.style.top = position.y;

            DropLocation? location = FindDropLocation(data, position, actionKey, shiftKey, out IIODropTarget? foundTarget);
            SetHoveredTarget(foundTarget);
            foundTarget?.OnDragOver(data, position, location != null);

            source.OnDragUpdate(data, location != null, location?.operation);
        }

        void SetHoveredTarget(IIODropTarget? dropTarget)
        {
            if (hoveredTarget == dropTarget)
                return;

            hoveredTarget?.OnDragLeave();
            hoveredTarget = dropTarget;
        }

        void End()
        {
            IODragData? endedData = data;
            data = null;

            // 끄지 않고 누르기만 했으면 정리할 것이 없습니다.
            if (ghost == null || endedData == null)
                return;

            SetHoveredTarget(null);

            // 포인터를 놓을 때 오는 PointerCaptureOut이 다시 끝내지 않도록, 고스트를 먼저 지웁니다.
            ghost.RemoveFromHierarchy();
            ghost = null;

            root?.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            root?.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            root = null;

            target.ReleasePointer(pointerId);
            source.OnDragEnd(endedData);
        }

        // 고스트는 포인터를 받지 않으므로, 포인터 아래 요소부터 조상으로 올라가며 처음 만나는 놓을 대상에 묻습니다.
        // foundTarget은 놓을 수 없더라도 포인터 아래에 있는 놓을 대상입니다.
        DropLocation? FindDropLocation(IODragData data, Vector2 position, bool actionKey, bool shiftKey, out IIODropTarget? foundTarget)
        {
            for (VisualElement? element = root?.panel?.Pick(position); element != null; element = element.parent)
            {
                if (element is not IIODropTarget dropTarget)
                    continue;

                foundTarget = dropTarget;
                if (!dropTarget.TryGetDropDirectory(position, out IONode directory)
                    || !TryGetOperation(data, directory,actionKey, shiftKey, out IODropOperation? operation))
                    return null;

                return new DropLocation(dropTarget, element, directory, operation);
            }

            foundTarget = null;
            return null;
        }

        // operation이 null이면 보조 키가 없어 놓을 때 메뉴로 묻습니다. 놓을 수 없으면 false입니다.
        static bool TryGetOperation(IODragData data, IONode directory, bool actionKey, bool shiftKey, out IODropOperation? operation)
        {
            operation = (actionKey, shiftKey) switch
            {
                (false, true) => IODropOperation.Move,
                (true, false) => IODropOperation.Copy,
                (true, true) => IODropOperation.Link,
                _ => null
            };

            return operation switch
            {
                IODropOperation.Move => CanMove(data, directory),
                IODropOperation.Copy => CanCopy(data, directory),
                IODropOperation.Link => CanLink(data, directory),

                // 드래그를 시작했으면 어디에 놓든 존중합니다. 같은 폴더여도 복사나 링크를 원할 수 있으므로 묻고, 이동은 CanMove가 막습니다.
                _ => CanMove(data, directory) || CanCopy(data, directory)
            };
        }

        static bool CanCopy(IODragData data, IONode directory) => IOTransfer.CanCopy(data.provider, data.entries, directory);

        static bool CanMove(IODragData data, IONode directory) => data.source.canWrite && IOTransfer.CanMove(data.provider, data.entries, directory);

        static bool CanLink(IODragData data, IONode directory) => IOTransfer.CanLink(data.provider, directory);

        static string GetText(string key) => LocalizationUtility.GetText(key) ?? key;

        static string GetOperationText(IODropOperation operation) => GetText(operation switch
        {
            IODropOperation.Move => "runios-editor:io_node_tree_view.drop.move",
            IODropOperation.Copy => "runios-editor:io_node_tree_view.drop.copy",
            _ => "runios-editor:io_node_tree_view.drop.link"
        });

        static void ShowMenu(Vector2 position, IODragData data, DropLocation location)
        {
            RuniMenu menu = new();

            AddItem(IODropOperation.Move, moveShortcut, CanMove(data, location.directory));
            AddItem(IODropOperation.Copy, copyShortcut, CanCopy(data, location.directory));
            AddItem(IODropOperation.Link, linkShortcut, CanLink(data, location.directory));
            menu.Add(new Separator());
            menu.Add(new RuniMenuItem(GetText("runios-editor:io_node_tree_view.drop.cancel"), null) { shortcuts = [cancelShortcut] });

            menu.DropDown(new Rect(position, Vector2.zero), location.element);

            void AddItem(IODropOperation operation, KeyShortcut shortcut, bool enabled)
            {
                RuniMenuItem item = new(GetOperationText(operation), () => Drop(data, location, operation).Forget()) { shortcuts = [shortcut] };
                item.SetEnabled(enabled);
                menu.Add(item);
            }
        }

        static async UniTask Drop(IODragData data, DropLocation location, IODropOperation operation)
        {
            if (location.directory.AsWriteNode() is not { } directory)
                return;

            await IOTransfer.Run(data.provider, data.entries, directory, operation, location.element);

            // 새로 고침은 펼침·선택·스크롤을 유지하므로, 바뀐 내용만 반영됩니다.
            // 사이드바처럼 목록이 아닌 곳에 놓아도 놓은 폴더가 원본 목록에 펼쳐져 있을 수 있으므로, 원본은 항상 새로 고칩니다.
            data.source.Refresh().Forget();
            if (location.target is IONodeMultiColumnTreeView targetTreeView && targetTreeView != data.source)
                targetTreeView.Refresh().Forget();
            else if (location.target is IOBrowserTabView tabView)
                tabView.Refresh(location.directory);
        }
    }
}
