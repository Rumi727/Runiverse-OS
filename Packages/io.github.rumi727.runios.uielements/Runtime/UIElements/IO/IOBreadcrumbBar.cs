#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// 경로의 각 부분을 버튼으로 보여 주는 경로 표시줄입니다. 버튼 사이의 <c>&gt;</c>를 누르면 그 폴더의 하위 폴더 메뉴가 열리고, 각 버튼은 <see cref="IIODropTarget"/>입니다.
    /// </summary>
    public class IOBreadcrumbBar : VisualElement
    {
        public const string ussClassName = "runios-io-breadcrumb-bar";
        public const string scrollViewUssClassName = ussClassName + "__scroll-view";
        public const string contentUssClassName = ussClassName + "__content";
        public const string crumbUssClassName = ussClassName + "__crumb";
        public const string crumbDropTargetUssClassName = crumbUssClassName + "--drop-target";
        public const string arrowUssClassName = ussClassName + "__arrow";
        public const string blankUssClassName = ussClassName + "__blank";
        public const string editButtonUssClassName = ussClassName + "__edit-button";
        public const string menuItemDropTargetUssClassName = ussClassName + "__menu-item--drop-target";

        // 끄는 항목을 > 위에 이만큼 두면 하위 폴더 메뉴가 열립니다.
        const long dragOpenDelay = 500;

        // 놓은 뒤 동작을 묻는 메뉴가 놓을 대상인 이 메뉴를 기준으로 열리므로, 이 메뉴는 계층에 붙여 두고 숨깁니다.
        // 메뉴 항목은 닫히며 팝업이 원래 자리로 돌아갈 때 패널에서 떨어지므로 기준이 될 수 없습니다.
        readonly FolderMenu folderMenu = new() { name = "folder-menu", style = { display = DisplayStyle.None } };

        public ScrollView scrollView { get; }

        /// <summary>
        /// 누르면 <see cref="editRequested"/>가 발생하는 버튼입니다.
        /// </summary>
        public Button editButton { get; }

        /// <summary>
        /// 경로 버튼 뒤의 빈 곳입니다. 누르면 <see cref="editRequested"/>가 발생합니다.
        /// </summary>
        public VisualElement blank { get; }

        /// <summary>
        /// 경로 버튼이나 하위 폴더 메뉴 항목을 골라 그 폴더로 이동하려 하면 발생합니다.
        /// </summary>
        public event Action<IONode>? nodeSelected;

        /// <summary>
        /// 빈 곳을 눌러 경로를 직접 입력하려 하면 발생합니다.
        /// </summary>
        public event Action? editRequested;

        public IOBreadcrumbBar()
        {
            AddToClassList(ussClassName);

            scrollView = new ScrollView(ScrollViewMode.Horizontal) { name = "scroll-view", horizontalScrollerVisibility = ScrollerVisibility.Hidden };
            scrollView.AddToClassList(scrollViewUssClassName);
            scrollView.contentContainer.AddToClassList(contentUssClassName);
            Add(scrollView);

            editButton = new Button(() => editRequested?.Invoke()) { name = "edit-button" };
            editButton.AddToClassList(editButtonUssClassName);
            editButton.AddManipulator(new AssetScopeManipulator<Texture2D>
            {
                assetRef = "runios:ui/io_browser/edit",
                editorAssetRef = "runios-editor:ui/io_browser/edit",
                editorDarkAssetRef = "runios-editor:ui_dark/io_browser/edit",
                applyAsset = x => editButton.iconImage = x,
                clearAsset = () => editButton.iconImage = default
            });
            Add(editButton);

            Add(folderMenu);

            blank = new VisualElement { name = "blank" };
            blank.AddToClassList(blankUssClassName);
            blank.AddManipulator(new Clickable(() => editRequested?.Invoke()));

            // 현재 폴더(맨 뒤 버튼)가 항상 보이도록, 내용이 바뀌어 길이가 달라지면 끝으로 스크롤합니다.
            scrollView.contentContainer.RegisterCallback<GeometryChangedEvent>(_ =>
                scrollView.schedule.Execute(() => scrollView.scrollOffset = new Vector2(scrollView.horizontalScroller.highValue, 0)));
        }

        /// <summary>
        /// 경로 버튼을 지정한 목록으로 교체합니다.
        /// </summary>
        public void SetCrumbs(IEnumerable<(string name, IONode node)> crumbs)
        {
            scrollView.Clear();

            // > 는 경로 버튼 사이에만 두므로, 마지막 버튼 뒤에는 없습니다.
            IONode? previous = null;
            foreach ((string name, IONode node) in crumbs)
            {
                if (previous is { } parent)
                    AddArrow(parent);

                scrollView.Add(new Crumb(name, node, x => nodeSelected?.Invoke(x)));
                previous = node;
            }

            scrollView.Add(blank);
        }

        void AddArrow(IONode node)
        {
            Arrow arrow = new();
            arrow.dragHovered = () => DropDownFolders(node, arrow, true).Forget();
            arrow.clicked += () => DropDownFolders(node, arrow, false).Forget();
            scrollView.Add(arrow);

            // 하위 폴더가 없으면 눌러도 열 것이 없으므로 비활성화합니다.
            UpdateArrowEnabled(node, arrow).Forget();
        }

        static async UniTaskVoid UpdateArrowEnabled(IONode node, Arrow arrow) => arrow.SetEnabled(await node.dir.GetDirectories().AnyAsync());

        // 끄는 중에 열었으면 메뉴 항목에도 놓을 수 있고, 놓거나 취소해 포인터를 놓으면 닫습니다.
        async UniTaskVoid DropDownFolders(IONode node, Arrow arrow, bool dragging)
        {
            List<string> names = [];
            await foreach (IOEntry entry in node.dir.GetDirectories())
                names.Add(entry.path.GetFileName());

            if (names.Count <= 0)
                return;

            names.Sort(StringComparer.OrdinalIgnoreCase);

            folderMenu.Close(false);
            folderMenu.Clear();
            foreach (string name in names)
                {
                IONode child = node.CreateChild(name);
                folderMenu.Add(new RuniMenuItem(name, () => nodeSelected?.Invoke(child)) { userData = child });
            }

            // 끄는 중에는 뒤의 경로 버튼에도 놓을 수 있도록 배경을 막지 않고 엽니다.
            // 드래그 고스트는 패널 맨 위에 붙어 있으므로, 메뉴의 오버레이가 고스트를 가리지 않게 연 뒤에 다시 맨 위로 올립니다.
            VisualElement root = arrow.panel.visualTree;
            VisualElement? ghost = dragging ? root[root.childCount - 1] : null;
            folderMenu.DropDown(arrow.worldBound, arrow, !dragging);
            ghost?.BringToFront();

            if (dragging)
            {
                root.RegisterCallback<PointerUpEvent>(CloseMenu, TrickleDown.TrickleDown);
                root.RegisterCallback<PointerCaptureOutEvent>(CloseMenu, TrickleDown.TrickleDown);

                // 놓는 처리가 끝난 뒤에 닫도록 미룹니다.
                void CloseMenu<T>(T _) where T : EventBase<T>, new()
                {
                    root.UnregisterCallback<PointerUpEvent>(CloseMenu);
                    root.UnregisterCallback<PointerCaptureOutEvent>(CloseMenu);
                    arrow.schedule.Execute(() => folderMenu.Close(false));
                }
            }
        }

        // 끄는 항목을 > 위에 두면 dragHovered가 호출됩니다. > 자체에는 놓을 수 없습니다.
        sealed class Arrow : Button, IIODropTarget
        {
            public Action? dragHovered { get; set; }

            IVisualElementScheduledItem? dragHoverSchedule;

            public Arrow()
            {
                name = "arrow";
                text = ">";
                AddToClassList(arrowUssClassName);
            }

            bool IIODropTarget.TryGetDropDirectory(Vector2 position, out IONode directory)
            {
                directory = default;
                return false;
            }

            void IIODropTarget.OnDragOver(IODragData data, Vector2 position, bool canDrop) => dragHoverSchedule ??= schedule.Execute(() => dragHovered?.Invoke()).StartingIn(dragOpenDelay);

            void IIODropTarget.OnDragLeave()
            {
                dragHoverSchedule?.Pause();
                dragHoverSchedule = null;
            }
        }

        // 메뉴 항목의 논리적 부모가 이 메뉴이므로, 항목 위에 놓으면 이 메뉴가 놓을 대상으로 찾아집니다. 항목의 폴더는 userData에 있습니다.
        sealed class FolderMenu : RuniMenu, IIODropTarget
        {
            VisualElement? highlighted;

            bool IIODropTarget.TryGetDropDirectory(Vector2 position, out IONode directory)
            {
                RuniMenuItem? item = GetItem(position);
                directory = item?.userData is IONode node ? node : default;
                return item != null;
            }

            void IIODropTarget.OnDragOver(IODragData data, Vector2 position, bool canDrop) => SetHighlight(canDrop ? GetItem(position) : null);

            void IIODropTarget.OnDragLeave() => SetHighlight(null);

            RuniMenuItem? GetItem(Vector2 position)
            {
                for (VisualElement? element = panel?.Pick(position); element != null && element != this; element = element.parent)
                {
                    if (element is RuniMenuItem item)
                        return item;
                }

                return null;
            }

            void SetHighlight(VisualElement? element)
            {
                if (highlighted == element)
                    return;

                highlighted?.RemoveFromClassList(menuItemDropTargetUssClassName);
                highlighted = element;
                highlighted?.AddToClassList(menuItemDropTargetUssClassName);
            }
        }

        sealed class Crumb : Button, IIODropTarget
        {
            readonly IONode _node;

            public Crumb(string text, IONode node, Action<IONode> selected) : base(() => selected(node))
            {
                _node = node;

                name = "crumb";
                this.text = text;
                AddToClassList(crumbUssClassName);
            }

            // 홈 화면(장치 목록)에는 놓을 수 없습니다.
            bool IIODropTarget.TryGetDropDirectory(Vector2 position, out IONode directory)
            {
                directory = _node;
                return _node != IONode.empty;
            }

            void IIODropTarget.OnDragOver(IODragData data, Vector2 position, bool canDrop) => EnableInClassList(crumbDropTargetUssClassName, canDrop);

            void IIODropTarget.OnDragLeave() => RemoveFromClassList(crumbDropTargetUssClassName);
        }
    }
}
