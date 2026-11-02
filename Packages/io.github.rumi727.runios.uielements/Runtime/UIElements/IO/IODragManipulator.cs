#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// 대상 요소를 누른 채 끌면 IO 항목을 끌고, 놓으면 포인터 아래 요소부터 조상으로 올라가며 찾은 <see cref="IIODropTarget"/>에 놓습니다.
    /// 끄는 동안 포인터를 잡으므로, 같은 패널 안에서만 놓을 수 있습니다. Esc를 누르면 취소합니다.
    /// </summary>
    /// <remarks>
    /// Shift를 누르고 놓으면 이동하고, 동작 키(Ctrl·Command)를 누르고 놓으면 복사하며, 보조 키가 없으면 놓은 자리에서 메뉴로 묻습니다.
    /// 이동은 같은 프로바이더 안에서만 할 수 있습니다.
    /// </remarks>
    /// <param name="source">끌 항목을 정하고 고스트를 만드는 요소입니다. 보통 이 조작기를 붙이는 요소 자신입니다.</param>
    public sealed class IODragManipulator(IIODragSource source) : Manipulator
    {
        // 클릭하다 조금 움직인 것을 드래그로 보지 않도록, 이만큼 움직여야 시작합니다.
        const float startDistance = 8;

        // 놓을 때 누르고 있으면 메뉴 없이 바로 그 동작을 하는 보조 키입니다.
        static readonly KeyShortcut moveShortcut = new(KeyCode.None, modifiers: EventModifiers.Shift);
        static readonly KeyShortcut copyShortcut = new(KeyCode.None, actionKey: true);
        static readonly KeyShortcut cancelShortcut = new(KeyCode.Escape);

        // element는 target인 요소이며, 놓을 때 메뉴를 그 요소에 엽니다. operation이 null이면 놓을 때 메뉴로 묻습니다.
        readonly record struct DropLocation(IIODropTarget target, VisualElement element, IONode directory, IODropOperation? operation);

        IODragData? data;
        int pointerId;
        Vector2 startPosition;
        Vector2 lastPosition;

        // 끄는 동안에만 있습니다.
        VisualElement? ghost;
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
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            target.UnregisterCallback<PointerMoveEvent>(OnPointerMove);
            target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<KeyUpEvent>(OnKeyUp);

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

            // 놓은 뒤 메뉴를 열거나 작업을 시작하므로, 드래그를 먼저 끝냅니다.
            IODragData droppedData = data;
            DropLocation? location = FindDropLocation(droppedData, evt.position, evt.actionKey, evt.shiftKey, out _);
            End();

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

            target.panel.visualTree.Add(ghost);
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

            target.ReleasePointer(pointerId);
            source.OnDragEnd(endedData);
        }

        // 고스트는 포인터를 받지 않으므로, 포인터 아래 요소부터 조상으로 올라가며 처음 만나는 놓을 대상에 묻습니다.
        // foundTarget은 놓을 수 없더라도 포인터 아래에 있는 놓을 대상입니다.
        DropLocation? FindDropLocation(IODragData data, Vector2 position, bool actionKey, bool shiftKey, out IIODropTarget? foundTarget)
        {
            for (VisualElement? element = target.panel.Pick(position); element != null; element = element.parent)
            {
                if (element is not IIODropTarget dropTarget)
                    continue;

                foundTarget = dropTarget;
                if (!dropTarget.TryGetDropDirectory(position, out IONode directory)
                    || !TryGetOperation(data, directory, actionKey, shiftKey, out IODropOperation? operation))
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
                _ => null
            };

            return operation switch
            {
                IODropOperation.Move => CanMove(data, directory),
                IODropOperation.Copy => CanCopy(data, directory),

                // 같은 프로바이더에서 이동할 수 없으면 원래 있던 폴더에 조금 끌다 놓은 것이므로 묻지 않습니다. 복사하려면 동작 키를 누릅니다.
                _ => CanMove(data, directory) || (CanCopy(data, directory) && !data.provider.IsSameTarget(directory.provider))
            };
        }

        static bool CanCopy(IODragData data, IONode directory) =>
            directory.provider is IWritableIOProvider

            // 폴더를 자기 자신이나 그 안으로 넣으면 끝나지 않습니다.
            && !(data.provider.IsSameTarget(directory.provider) && data.entries.Any(x => x.isDirectory && directory.path.StartsWith(x.path)));

        // 이미 그 폴더에 있는 항목만 옮기면 바뀌는 것이 없습니다.
        static bool CanMove(IODragData data, IONode directory) =>
            CanCopy(data, directory)
            && data.provider.IsSameTarget(directory.provider)
            && data.provider is IWritableIOProvider
            && !data.source.readOnly
            && data.entries.Any(x => x.path.GetParentPath() != directory.path);

        static string GetText(string key) => LocalizationUtility.GetText(key) ?? key;

        static string GetOperationText(IODropOperation operation) =>
            GetText(operation == IODropOperation.Move ? "runios-editor:io_node_tree_view.drop.move" : "runios-editor:io_node_tree_view.drop.copy");

        static void ShowMenu(Vector2 position, IODragData data, DropLocation location)
        {
            RuniMenu menu = new();

            AddItem(IODropOperation.Move, moveShortcut, CanMove(data, location.directory));
            AddItem(IODropOperation.Copy, copyShortcut, CanCopy(data, location.directory));
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
            IONode directory = location.directory;
            if (directory.provider is not IWritableIOProvider destinationProvider)
                return;

            bool sameProvider = data.provider.IsSameTarget(destinationProvider);
            foreach (IOEntry entry in data.entries)
            {
                // 이미 그 폴더에 있는 항목은 옮겨도 바뀌지 않으므로 건너뜁니다.
                if (operation == IODropOperation.Move && entry.path.GetParentPath() == directory.path)
                    continue;

                RuniPath destinationPath = directory.path / entry.path.GetFileName();

                // 대상이 이미 있는지는 덮어쓰지 않는 이동·복사가 판단하므로 미리 확인하지 않습니다.
                // 같은 프로바이더면 같은 대상을 가리키므로, 이동과 복사를 대상 프로바이더에서 수행합니다.
                try
                {
                    if (!sameProvider)
                        await CopyAcrossProviders(data.provider, entry, destinationProvider, destinationPath);
                    else if (operation == IODropOperation.Move)
                        await (entry.isDirectory ? destinationProvider.MoveDirectory(entry.path, destinationPath) : destinationProvider.MoveFile(entry.path, destinationPath));
                    else
                        await (entry.isDirectory ? destinationProvider.CopyDirectory(entry.path, destinationPath) : destinationProvider.CopyFile(entry.path, destinationPath));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            // 새로 고침은 펼침·선택·스크롤을 유지하므로, 바뀐 내용만 반영됩니다.
            // 사이드바처럼 목록이 아닌 곳에 놓아도 놓은 폴더가 원본 목록에 펼쳐져 있을 수 있으므로, 원본은 항상 새로 고칩니다.
            data.source.Refresh().Forget();
            if (location.target is IONodeMultiColumnTreeView targetTreeView && targetTreeView != data.source)
                targetTreeView.Refresh().Forget();
        }

        // 다른 프로바이더로는 프로바이더의 복사를 쓸 수 없으므로, 원본을 읽어 대상에 새로 씁니다.
        static async UniTask CopyAcrossProviders(IIOProvider sourceProvider, IOEntry entry, IWritableIOProvider destinationProvider, RuniPath destinationPath)
        {
            if (await destinationProvider.GetEntry(destinationPath) != null)
                throw new IOException($"The destination '{destinationPath}' already exists.");

            if (!entry.isDirectory)
            {
                await using Stream source = await sourceProvider.OpenRead(entry.path);
                await using Stream destination = await destinationProvider.CreateFile(destinationPath);
                await source.CopyToAsync(destination);

                return;
            }

            await destinationProvider.CreateDirectory(destinationPath);
            await foreach (IOEntry child in sourceProvider.EnumerateEntries(entry.path, false))
                await CopyAcrossProviders(sourceProvider, child, destinationProvider, destinationPath / child.path.GetFileName());
        }
    }
}
