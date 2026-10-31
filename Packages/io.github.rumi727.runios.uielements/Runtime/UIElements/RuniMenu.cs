#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// 항목 목록을 팝업으로 여는 메뉴입니다. 아무 <see cref="VisualElement"/>나 넣을 수 있고, 넣은 요소는 자기 규칙대로 동작합니다.
    /// <see cref="RuniMenuItem"/>은 클릭하면 메뉴를 닫는 일반적인 항목이고, <see cref="RuniMenu"/>를 넣으면 중첩 메뉴가 됩니다.
    /// <br/>
    /// 어디에도 붙이지 않은 채 <see cref="DropDown"/>으로 열 수 있고(우클릭 메뉴 등), 일반 계층에 붙이면 헤더를 클릭해 여는 메뉴 버튼이 되며,
    /// 다른 <see cref="RuniMenu"/> 안에 넣으면 호버·포커스로 옆에 열리는 하위 메뉴가 됩니다.
    /// <br/>
    /// 닫혀 있는 동안 팝업은 이 요소의 계층 안에 있고(<see cref="expanded"/>가 아니면 숨김), 열면 패널 전체를 덮는 오버레이로 옮겨집니다.
    /// 페이드 같은 전환은 <see cref="popupFloatingUssClassName"/>에 transition을 선언하고 <see cref="popupOpenUssClassName"/>에 목표 값을 두면 됩니다.
    /// 팝업은 전환이 끝난 뒤에 원래 자리로 돌아옵니다.
    /// </summary>
    [UxmlElement]
    public partial class RuniMenu : VisualElement
    {
        public const string ussClassName = "runios-menu";
        /// <summary>다른 <see cref="RuniMenu"/>의 팝업 안에 있을 때 붙습니다.</summary>
        public const string nestedUssClassName = ussClassName + "--nested";
        /// <summary><see cref="expanded"/>가 <see langword="true"/>일 때 붙습니다.</summary>
        public const string expandedUssClassName = ussClassName + "--expanded";
        /// <summary>팝업이 열려 있는 동안 붙습니다. 헤더 강조용입니다.</summary>
        public const string openUssClassName = ussClassName + "--open";
        public const string headerUssClassName = ussClassName + "__header";
        public const string iconUssClassName = ussClassName + "__icon";
        public const string labelUssClassName = ussClassName + "__label";
        public const string arrowUssClassName = ussClassName + "__arrow";
        public const string overlayUssClassName = ussClassName + "__overlay";
        public const string popupUssClassName = ussClassName + "__popup";
        /// <summary>팝업이 오버레이에 있는 동안(닫히는 전환 중 포함) 붙습니다. transition은 여기에 선언하는 것을 권장합니다.</summary>
        public const string popupFloatingUssClassName = popupUssClassName + "--floating";
        /// <summary>팝업이 열려 있는 동안 붙습니다. 처음 배치된 뒤에 붙고, 닫기를 시작하자마자 떨어집니다.</summary>
        public const string popupOpenUssClassName = popupUssClassName + "--open";
        public const string scrollViewUssClassName = ussClassName + "__scroll-view";
        public const string contentUssClassName = ussClassName + "__content";
        /// <summary>
        /// 메뉴의 직계 자식에 붙습니다. 팝업이 오버레이에 떠 있는 동안에는 자식의 논리적 부모(이 메뉴)가 계층상 조상이 아니어서
        /// 조상 선택자로 자식을 고를 수 없으므로, 메뉴 자식은 이 클래스로 고릅니다.
        /// </summary>
        public const string childUssClassName = ussClassName + "__child";

        [UxmlAttribute]
        public string text
        {
            get => label.text;
            set => label.text = value;
        }

        /// <summary>
        /// 닫혀 있는 동안 제자리의 팝업을 보여 줄지 여부입니다. UI Builder에서 항목을 보면서 편집할 때 씁니다.
        /// </summary>
        [UxmlAttribute]
        public bool expanded
        {
            get;
            set
            {
                field = value;
                EnableInClassList(expandedUssClassName, value);
            }
        }

        /// <summary>
        /// 하위 메뉴일 때, 호버하거나 포커스를 받은 뒤 열릴 때까지의 시간(ms)입니다.
        /// </summary>
        [UxmlAttribute] public long openDelay { get; set; } = 500;

        /// <summary>
        /// 열린 하위 메뉴가 있을 때, 다른 항목으로 옮겨 간 뒤 그 하위 메뉴가 닫힐 때까지의 시간(ms)입니다.
        /// </summary>
        [UxmlAttribute] public long closeDelay { get; set; }

        [UxmlAttribute] public bool openOnHover { get; set; } = true;
        [UxmlAttribute] public bool openOnFocus { get; set; } = true;

        /// <summary>
        /// <see cref="DropDown"/>으로 열 때의 너비 규칙입니다. 의미는 <see cref="GenericDropdownMenu"/>와 같습니다.
        /// </summary>
        [UxmlAttribute] public DropdownMenuSizeMode sizeMode { get; set; } = DropdownMenuSizeMode.Auto;

        public VisualElement header { get; }
        public Image icon { get; }
        public AssetScopeManipulator<VectorImage> iconManipulator { get; }
        public Label label { get; }
        public VisualElement arrow { get; }

        public VisualElement popup => _popup;
        readonly Popup _popup;
        readonly ScrollView scrollView;

        public override VisualElement contentContainer { get; }

        /// <summary>
        /// 이 요소가 들어 있는 팝업의 메뉴입니다. 다른 <see cref="RuniMenu"/> 안에 있지 않으면 <see langword="null"/>입니다.
        /// </summary>
        public RuniMenu? parentMenu { get; private set; }

        public bool isOpen { get; private set; }

        /// <summary>
        /// 팝업을 열기 직전, 배치되기 전에 발생합니다. 열 때마다 항목 상태를 갱신하는 데 씁니다.
        /// </summary>
        public event Action? opened;

        /// <summary>
        /// 닫기를 시작하면 발생합니다. 팝업은 전환이 끝난 뒤에 원래 자리로 돌아옵니다.
        /// </summary>
        public event Action? closed;

        readonly TransitionManipulator popupTransitions = new();
        readonly List<VisualElement> previousChildren = [];

        // 하위 메뉴로 열렸을 때의 부모입니다. DropDown으로 열렸으면 null입니다.
        RuniMenu? openParent;
        RuniMenu? openSubMenu;

        // DropDown으로 열렸을 때만 씁니다.
        VisualElement? overlay;
        VisualElement? targetElement;
        Rect dropDownPosition;

        bool focusFirstOnOpen;

        IVisualElementScheduledItem? openSchedule;
        IVisualElementScheduledItem? closeSubMenuSchedule;
        CancellationTokenSource? closingCancellation;

        public RuniMenu() : this(string.Empty) { }

        public RuniMenu(string text)
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            focusable = true;

            header = new VisualElement { name = "header" };
            header.AddToClassList(headerUssClassName);
            hierarchy.Add(header);

            icon = new Image { name = "icon" };
            icon.AddToClassList(iconUssClassName);
            iconManipulator = new AssetScopeManipulator<VectorImage>
            {
                applyAsset = x => icon.vectorImage = x,
                clearAsset = () => icon.vectorImage = null
            };
            icon.AddManipulator(iconManipulator);
            header.Add(icon);

            label = new Label(text) { name = "label" };
            label.AddToClassList(labelUssClassName);
            header.Add(label);

            arrow = new VisualElement { name = "arrow" };
            arrow.AddToClassList(arrowUssClassName);
            header.Add(arrow);

            _popup = new Popup(this) { name = "popup" };
            _popup.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            _popup.AddToClassList(popupUssClassName);
            _popup.AddManipulator(popupTransitions);
            hierarchy.Add(_popup);

            // 버튼이나 필드처럼 넓은 요소도 들어갈 수 있으므로, 패널보다 넓으면 가로로도 스크롤합니다.
            scrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal) { name = "scroll-view" };
            scrollView.AddToClassList(scrollViewUssClassName);
            _popup.Add(scrollView);

            contentContainer = scrollView.contentContainer;
            contentContainer.AddToClassList(contentUssClassName);
            contentContainer.focusable = true;
            contentContainer.AddManipulator(new KeyboardNavigationManipulator(OnNavigation));
            contentContainer.RegisterCallback<PointerMoveEvent>(OnContentPointerMove);
            contentContainer.RegisterCallback<FocusInEvent>(OnContentFocusIn);
            contentContainer.AddManipulator(new ChildrenChangedManipulator(UpdateChildClasses));

            // 열려 있는 동안 내용, 스크롤바, 팝업 위치가 바뀌면 크기와 위치를 다시 맞춥니다.
            _popup.RegisterCallback<GeometryChangedEvent>(OnLayoutChanged);
            contentContainer.RegisterCallback<GeometryChangedEvent>(OnLayoutChanged);
            scrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(OnLayoutChanged);

            // 닫히는 전환 중에는 항목이 눌리지 않게 합니다.
            _popup.RegisterCallback<PointerDownEvent>(OnPopupPointerDown, TrickleDown.TrickleDown);
            _popup.RegisterCallback<PointerEnterEvent>(OnPopupPointerEnter);

            header.AddManipulator(new Clickable(OnHeaderClicked));
            header.RegisterCallback<PointerEnterEvent>(OnHeaderPointerEnter);
            header.RegisterCallback<PointerLeaveEvent>(OnHeaderPointerLeave);

            RegisterCallback<FocusInEvent>(OnFocusIn);
            RegisterCallback<FocusOutEvent>(OnFocusOut);
            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        void UpdateChildClasses()
        {
            // 다른 메뉴로 옮겨 간 자식은 그 메뉴가 붙인 클래스를 떼지 않습니다.
            foreach (VisualElement child in previousChildren)
            {
                if (child.hierarchy.parent is not { } parent || !parent.ClassListContains(contentUssClassName))
                    child.RemoveFromClassList(childUssClassName);
            }

            previousChildren.Clear();

            foreach (VisualElement child in contentContainer.Children())
            {
                child.AddToClassList(childUssClassName);
                previousChildren.Add(child);
            }
        }

        /// <summary>
        /// <paramref name="element"/>가 들어 있는 팝업의 메뉴를 가져옵니다. 팝업 안에 있지 않으면 <see langword="null"/>입니다.
        /// </summary>
        public static RuniMenu? GetMenu(VisualElement element)
        {
            for (VisualElement? current = element; current != null; current = current.hierarchy.parent)
            {
                if (current is Popup popup)
                    return popup.menu;
            }

            return null;
        }

        /// <summary>
        /// <paramref name="position"/> 아래에 팝업을 엽니다. 팝업은 <paramref name="targetElement"/>가 있는 패널에 표시됩니다.
        /// </summary>
        /// <param name="position">패널 좌표계의 기준 영역입니다.</param>
        /// <param name="targetElement">팝업을 표시할 패널을 정하고, 닫을 때 포커스를 돌려받는 요소입니다.</param>
        public void DropDown(Rect position, VisualElement targetElement)
        {
            if (targetElement.panel is not { } panel)
                return;

            if (isOpen)
                Close(false);

            // 닫히는 전환을 기다리는 중이면, 오버레이를 다시 붙이다가 그 대기가 끝나 오버레이가 떼어지지 않도록 먼저 취소합니다.
            closingCancellation?.Cancel();
            closingCancellation = null;

            this.targetElement = targetElement;
            targetElement.RegisterCallback<DetachFromPanelEvent>(OnTargetDetach);

            if (overlay == null)
            {
                overlay = new VisualElement { name = "overlay" };
                overlay.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
                overlay.AddToClassList(overlayUssClassName);
                overlay.RegisterCallback<PointerDownEvent>(OnOverlayPointerDown);
                overlay.RegisterCallback<FocusOutEvent>(OnOverlayFocusOut);
                overlay.RegisterCallback<GeometryChangedEvent>(OnLayoutChanged);
            }

            overlay.pickingMode = PickingMode.Position;
            if (overlay.parent != panel.visualTree)
                panel.visualTree.Add(overlay);
            else
                overlay.BringToFront();

            dropDownPosition = position;
            Open(null, false);
        }

        /// <summary>
        /// 이 메뉴와 그 아래 하위 메뉴를 닫습니다.
        /// </summary>
        /// <param name="giveFocusBack">하위 메뉴면 헤더로, <see cref="DropDown"/>으로 열었으면 targetElement로 포커스를 돌려줄지 여부입니다.</param>
        public void Close(bool giveFocusBack = true)
        {
            if (!isOpen)
                return;

            openSubMenu?.Close(false);

            openSchedule?.Pause();
            closeSubMenuSchedule?.Pause();

            isOpen = false;
            RemoveFromClassList(openUssClassName);
            _popup.RemoveFromClassList(popupOpenUssClassName);

            closed?.Invoke();

            // 팝업은 원래 자리로 돌아가면 숨겨지므로, 포커스가 그 안에 남아 있지 않게 합니다.
            Focusable? focused = focusController?.focusedElement ?? _popup.focusController?.focusedElement;
            bool popupHasFocus = focused is VisualElement focusedElement && _popup.Contains(focusedElement);

            RuniMenu? parent = openParent;
            if (parent != null)
            {
                if (parent.openSubMenu == this)
                    parent.openSubMenu = null;

                if ((giveFocusBack || popupHasFocus) && canGrabFocus)
                    Focus();
            }
            else if (targetElement != null)
            {
                VisualElement target = targetElement;
                target.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetach);
                targetElement = null;

                if (overlay != null)
                    overlay.pickingMode = PickingMode.Ignore;

                if (giveFocusBack && target.canGrabFocus)
                    target.Focus();
                else if (popupHasFocus)
                    focused?.Blur();
            }

            ReturnPopupAfterTransitions().Forget();
        }

        /// <summary>
        /// 이 메뉴가 속한 가장 바깥 메뉴까지 모두 닫습니다.
        /// </summary>
        public void CloseAll(bool giveFocusBack = true)
        {
            RuniMenu root = this;
            while (root.openParent is { isOpen: true } parent)
                root = parent;

            root.Close(giveFocusBack);
        }

        // DropDown으로 열린 메뉴는 그 오버레이, 하위 메뉴는 부모가 쓰는 오버레이에 팝업을 둡니다.
        VisualElement? hostOverlay => openParent != null ? openParent.hostOverlay : overlay;

        void Open(RuniMenu? parent, bool focusFirst)
        {
            closingCancellation?.Cancel();
            closingCancellation = null;

            openParent = parent;
            focusFirstOnOpen = focusFirst;

            if (hostOverlay is not { } host)
                return;

            isOpen = true;
            opened?.Invoke();

            AddToClassList(openUssClassName);

            bool wasFloating = _popup.parent == host;
            if (!wasFloating)
                host.Add(_popup);

            _popup.AddToClassList(popupFloatingUssClassName);

            // 다시 열 때 이미 오버레이에 있으면 레이아웃이 바뀌지 않아 GeometryChangedEvent가 오지 않으므로 바로 배치합니다.
            // 그 밖에는 처음 배치될 때 OnLayoutChanged가 배치합니다.
            if (wasFloating)
                OnPlaced();
        }

        // 스타일이 한 번 계산된 뒤에 popupOpenUssClassName을 붙여야 여는 전환이 실행됩니다.
        void OnPlaced()
        {
            if (!isOpen || hostOverlay is not { } host)
                return;

            Place(host);
            _popup.AddToClassList(popupOpenUssClassName);

            if (focusFirstOnOpen)
                MoveFocus(1);
            else if (openParent == null)
                contentContainer.Focus();
        }

        void OnLayoutChanged(GeometryChangedEvent evt)
        {
            if (!isOpen || hostOverlay is not { } host)
                return;

            // popupOpenUssClassName이 아직 없으면 오버레이로 옮긴 뒤 처음 배치되는 것입니다.
            if (!_popup.ClassListContains(popupOpenUssClassName))
                OnPlaced();
            else
                PlaceOpenTree(host);
        }

        // 하위 메뉴는 이 메뉴의 헤더 옆에 붙어 있으므로, 이 메뉴가 움직이면 함께 다시 배치합니다.
        void PlaceOpenTree(VisualElement host)
        {
            Place(host);

            if (openSubMenu is { isOpen: true } subMenu && subMenu._popup.ClassListContains(popupOpenUssClassName))
                subMenu.PlaceOpenTree(host);
        }

        void Place(VisualElement host)
        {
            Rect bounds = host.layout;
            IResolvedStyle resolved = _popup.resolvedStyle;

            // 팝업 크기는 내용 크기에서 직접 계산해, 열린 동안 내용이 바뀌어도 따라가고 패널보다 크면 스크롤되게 합니다.
            Vector2 content = contentContainer.layout.size;
            Vector2 desired = new(
                content.x + resolved.paddingLeft + resolved.paddingRight + resolved.borderLeftWidth + resolved.borderRightWidth,
                content.y + resolved.paddingTop + resolved.paddingBottom + resolved.borderTopWidth + resolved.borderBottomWidth);

            Rect anchor = host.WorldToLocal(openParent == null ? dropDownPosition : header.worldBound);

            // 스크롤바가 필요한지는 내용 크기와 패널 크기만으로 정합니다. 현재 스크롤바가 보이는지를 기준으로 삼으면
            // 크기를 바꿀 때마다 스크롤바가 켜졌다 꺼졌다 하므로, ScrollView의 Auto 판단에도 맡기지 않습니다.
            // 숨겨진 스크롤바의 두께는 0이므로, 처음 켤 때는 다음 배치에서 두께가 반영됩니다.
            bool needsVertical = desired.y > bounds.height;
            if (needsVertical)
                desired.x += scrollView.verticalScroller.layout.width;

            float width = GetWidth(desired.x, anchor, resolved, bounds);
            bool needsHorizontal = desired.x > width;
            if (needsHorizontal)
            {
                desired.y += scrollView.horizontalScroller.layout.height;

                if (!needsVertical && desired.y > bounds.height)
                {
                    needsVertical = true;
                    desired.x += scrollView.verticalScroller.layout.width;
                    width = GetWidth(desired.x, anchor, resolved, bounds);
                }
            }

            scrollView.verticalScrollerVisibility = needsVertical ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Hidden;
            scrollView.horizontalScrollerVisibility = needsHorizontal ? ScrollerVisibility.AlwaysVisible : ScrollerVisibility.Hidden;

            Vector2 size = new(width, Math.Min(desired.y, bounds.height));
            _popup.style.width = size.x;
            _popup.style.height = size.y;

            Vector2 position;
            if (openParent == null)
            {
                position = new Vector2(anchor.x, anchor.yMax);

                // 아래가 부족하면 위로 올리고, 위도 부족하면 아래 끝에 맞춥니다.
                if (position.y + size.y > bounds.height)
                    position.y = anchor.y - size.y >= 0 ? anchor.y - size.y : Math.Max(0, bounds.height - size.y);
            }
            else
            {
                // 첫 항목이 이 헤더와 같은 높이에 오도록 팝업 안쪽 여백만큼 올립니다.
                float contentOffset = scrollView.contentViewport.worldBound.y - _popup.worldBound.y;
                position = new Vector2(anchor.xMax, anchor.y - contentOffset);

                // 오른쪽이 부족하면 부모 쪽인 왼쪽에 엽니다.
                if (position.x + size.x > bounds.width)
                    position.x = anchor.x - size.x;

                if (position.y + size.y > bounds.height)
                    position.y = bounds.height - size.y;
            }

            if (position.x + size.x > bounds.width)
                position.x = bounds.width - size.x;

            position.x = Math.Max(0, position.x);
            position.y = Math.Max(0, position.y);

            // left와 top은 마진 상자 기준이 아니므로 마진만큼 빼 둡니다.
            _popup.style.left = position.x - resolved.marginLeft;
            _popup.style.top = position.y - resolved.marginTop;
        }

        float GetWidth(float desiredWidth, Rect anchor, IResolvedStyle resolved, Rect bounds)
        {
            float width = desiredWidth;
            if (openParent == null)
            {
                if (sizeMode == DropdownMenuSizeMode.Fixed)
                    width = anchor.width;
                else if (sizeMode == DropdownMenuSizeMode.Auto)
                    width = Math.Max(width, anchor.width);
            }

            // USS의 min-width는 그대로 존중합니다.
            StyleFloat minWidth = resolved.minWidth;
            if (minWidth.keyword == StyleKeyword.Undefined && !float.IsNaN(minWidth.value))
                width = Math.Max(width, minWidth.value);

            return Math.Min(width, bounds.width);
        }

        async UniTaskVoid ReturnPopupAfterTransitions()
        {
            CancellationTokenSource cancellation = new();
            closingCancellation = cancellation;

            bool canceled = await popupTransitions.WaitForTransitions(cancellation.Token).SuppressCancellationThrow();
            cancellation.Dispose();

            if (canceled)
                return;

            closingCancellation = null;

            _popup.RemoveFromClassList(popupFloatingUssClassName);
            _popup.style.left = StyleKeyword.Null;
            _popup.style.top = StyleKeyword.Null;
            _popup.style.width = StyleKeyword.Null;
            _popup.style.height = StyleKeyword.Null;
            hierarchy.Add(_popup);

            if (openParent == null)
                overlay?.RemoveFromHierarchy();

            openParent = null;
        }

        void OpenAsSubMenu(bool focusFirst)
        {
            if (parentMenu is not { isOpen: true } parent || !enabledInHierarchy)
                return;

            openSchedule?.Pause();
            parent.closeSubMenuSchedule?.Pause();

            if (isOpen)
            {
                if (focusFirst)
                    MoveFocus(1);

                return;
            }

            if (parent.openSubMenu != null && parent.openSubMenu != this)
                parent.openSubMenu.Close(false);

            parent.openSubMenu = this;
            Open(parent, focusFirst);
        }

        void ScheduleOpen()
        {
            if (isOpen || !enabledInHierarchy)
                return;

            openSchedule ??= schedule.Execute(() => OpenAsSubMenu(false));
            openSchedule.ExecuteLater(openDelay);
        }

        void OnHeaderClicked()
        {
            if (parentMenu != null)
                OpenAsSubMenu(false);
            else
                DropDown(header.worldBound, this);
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (evt.target != this)
                return;

            if (parentMenu != null)
                OpenAsSubMenu(true);
            else
                DropDown(header.worldBound, this);

            evt.StopPropagation();
        }

        void OnHeaderPointerEnter(PointerEnterEvent evt)
        {
            if (parentMenu != null && openOnHover)
                ScheduleOpen();
        }

        void OnHeaderPointerLeave(PointerLeaveEvent evt)
        {
            if (!isOpen)
                openSchedule?.Pause();
        }

        void OnFocusIn(FocusInEvent evt)
        {
            if (evt.target != this || parentMenu == null || !openOnFocus)
                return;

            // 하위 메뉴를 닫고 헤더로 돌아온 경우에는 다시 열지 않습니다.
            if (evt.relatedTarget is VisualElement previous && _popup.Contains(previous))
                return;

            ScheduleOpen();
        }

        void OnFocusOut(FocusOutEvent evt)
        {
            if (evt.target == this && !isOpen)
                openSchedule?.Pause();
        }

        void OnNavigation(KeyboardNavigationOperation operation, EventBase evt)
        {
            switch (operation)
            {
                case KeyboardNavigationOperation.Previous:
                    MoveFocus(-1);
                    break;
                case KeyboardNavigationOperation.Next:
                    MoveFocus(1);
                    break;
                case KeyboardNavigationOperation.Begin:
                case KeyboardNavigationOperation.PageUp:
                    FocusEdge(true);
                    break;
                case KeyboardNavigationOperation.End:
                case KeyboardNavigationOperation.PageDown:
                    FocusEdge(false);
                    break;
                case KeyboardNavigationOperation.MoveRight:
                    if (GetDirectChild(contentContainer.focusController?.focusedElement as VisualElement) is RuniMenu subMenu)
                        subMenu.OpenAsSubMenu(true);
                    break;
                case KeyboardNavigationOperation.MoveLeft:
                    if (openParent != null)
                        Close();
                    break;
                case KeyboardNavigationOperation.Cancel:
                    CloseAll();
                    break;
                default:
                    return;
            }

            evt.StopPropagation();
        }

        // 끝에 닿으면 반대쪽 끝으로 돌아갑니다.
        void MoveFocus(int direction)
        {
            List<VisualElement> children = contentContainer.Children().Where(static x => x.canGrabFocus).ToList();
            if (children.Count == 0)
                return;

            VisualElement? current = GetDirectChild(contentContainer.focusController?.focusedElement as VisualElement);
            int index = current != null ? children.IndexOf(current) : -1;

            int next = index < 0 ? (direction > 0 ? 0 : children.Count - 1) : (index + direction + children.Count) % children.Count;
            FocusChild(children[next]);
        }

        void FocusEdge(bool first)
        {
            VisualElement? child = first ? contentContainer.Children().FirstOrDefault(static x => x.canGrabFocus) : contentContainer.Children().LastOrDefault(static x => x.canGrabFocus);
            if (child != null)
                FocusChild(child);
        }

        void FocusChild(VisualElement child)
        {
            child.Focus();
            scrollView.ScrollTo(child);
        }

        VisualElement? GetDirectChild(VisualElement? element)
        {
            while (element != null && element.hierarchy.parent != contentContainer)
                element = element.hierarchy.parent;

            return element;
        }

        // 열린 하위 메뉴가 아닌 항목으로 옮겨 가면 closeDelay 뒤에 하위 메뉴를 닫습니다.
        void OnChildActivated(VisualElement? element)
        {
            if (openSubMenu == null)
                return;

            if (GetDirectChild(element) == openSubMenu)
            {
                closeSubMenuSchedule?.Pause();
                return;
            }

            // 포인터가 움직일 때마다 불리므로, 이미 예약되어 있으면 지연을 다시 늘리지 않습니다.
            if (closeSubMenuSchedule == null)
                closeSubMenuSchedule = _popup.schedule.Execute(() => openSubMenu?.Close(false)).StartingIn(closeDelay);
            else if (!closeSubMenuSchedule.isActive)
                closeSubMenuSchedule.ExecuteLater(closeDelay);
        }

        void OnContentPointerMove(PointerMoveEvent evt) => OnChildActivated(evt.target as VisualElement);

        void OnContentFocusIn(FocusInEvent evt) => OnChildActivated(evt.target as VisualElement);

        // 하위 메뉴의 팝업에 들어오면 그 하위 메뉴를 닫으려던 것을 취소합니다.
        void OnPopupPointerEnter(PointerEnterEvent evt)
        {
            if (evt.target == _popup && openParent is { } parent && parent.openSubMenu == this)
                parent.closeSubMenuSchedule?.Pause();
        }

        void OnPopupPointerDown(PointerDownEvent evt)
        {
            if (!isOpen && _popup.ClassListContains(popupFloatingUssClassName))
                evt.StopPropagation();
        }

        void OnOverlayPointerDown(PointerDownEvent evt)
        {
            if (evt.target != overlay)
                return;

            Close(false);
            evt.StopPropagation();
        }

        // Tab 등으로 포커스가 메뉴 밖의 다른 요소로 옮겨 가면 닫습니다.
        // 포커스가 그냥 해제된 경우(relatedTarget이 null)는 닫지 않습니다. 다시 열면서 이전 포커스를 해제하면
        // 그 FocusOutEvent가 처리 중인 이벤트 뒤로 미뤄져, 다시 연 메뉴를 닫아 버리기 때문입니다.
        void OnOverlayFocusOut(FocusOutEvent evt)
        {
            if (evt.relatedTarget is not VisualElement next || (overlay != null && overlay.Contains(next)))
                return;

            Close(false);
        }

        void OnTargetDetach(DetachFromPanelEvent evt) => Close(false);

        void OnAttach(AttachToPanelEvent evt)
        {
            if (evt.target != this)
                return;

            parentMenu = hierarchy.parent != null ? GetMenu(hierarchy.parent) : null;
            EnableInClassList(nestedUssClassName, parentMenu != null);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            if (evt.target != this)
                return;

            openSchedule?.Pause();
            Close(false);
        }

        sealed class Popup : VisualElement
        {
            public Popup(RuniMenu menu) => this.menu = menu;

            public RuniMenu menu { get; }
        }
    }
}
