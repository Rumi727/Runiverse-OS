#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.IO.Locations;
using RuniOS.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    public abstract class IOBrowser : VisualElement
    {
        public const string ussClassName = "runios-io-browser";
        public const string headerUssClassName = "runios-io-browser__header";

        public const string headerButtonUssClassName = "runios-io-browser__header-button";

        public const string navigationGroupUssClassName = "runios-io-browser__navigation-group";
        public const string navigationButtonUssClassName = "runios-io-browser__navigation-button";

        public const string backUssClassName = "runios-io-browser__back-button";
        public const string forwardUssClassName = "runios-io-browser__forward-button";
        public const string upUssClassName = "runios-io-browser__up-button";

        public const string pathPrefixLabelUssClassName = "runios-io-browser__path-prefix-label";
        public const string pathFieldUssClassName = "runios-io-browser__path-field";
        public const string breadcrumbBarUssClassName = "runios-io-browser__breadcrumb-bar";

        public const string folderMenuButtonUssClassName = "runios-io-browser__folder-menu";

        public const string navigationPaneUssClassName = "runios-io-browser__navigation-pane";
        public const string contentPaneUssClassName = "runios-io-browser__content-pane";

        public const string tabUssClassName = "runios-io-browser__tab";

        public IIOProvider provider => node.provider;
        public IONode node
        {
            get => activeTab.node;
            set => activeTab.node = value;
        }

        /// <summary>
        /// 이 브라우저 안에서 항목을 바꾸지 못하게 할지 정합니다. 장소와 경로 버튼에도 놓을 수 없게 됩니다.
        /// 모든 탭에 적용되고, 새로 여는 탭에도 적용됩니다.
        /// </summary>
        public bool readOnly
        {
            get;
            set
            {
                field = value;
                foreach (IOBrowserTab tab in tabs)
                    tab.readOnly = value;

                breadcrumbBar.readOnly = value;

                foreach (IOBrowserPlaceGroup group in navigationGroups)
                    group.readOnly = value;
            }
        }

        /// <summary>
        /// 내용 영역에 표시할 파일을 거릅니다. 폴더에는 적용되지 않으며, <see langword="null"/>이면 모든 파일을 표시합니다.
        /// 모든 탭에 적용되고, 새로 여는 탭에도 적용됩니다.
        /// </summary>
        public IPatternMatcher? filter
        {
            get;
            set
            {
                field = value;
                foreach (IOBrowserTab tab in tabs)
                    tab.filter = value;
            }
        }

        public IOBrowserTab activeTab => (IOBrowserTab)contentPane.activeTab;
        public IEnumerable<IOBrowserTab> tabs => contentPane.Children().Cast<IOBrowserTab>();

        /// <summary>
        /// 내용 영역에서 파일을 열면 발생합니다.
        /// </summary>
        public event Action<IONode>? fileOpened;

        /// <summary>
        /// 내용 영역에서 파일을 열 때 <see cref="fileOpened"/>를 발생시키는 것과 함께 파일을 실제로 열지 정합니다.
        /// 실제 경로가 있는 파일만 <see cref="IFileOpenService"/>로 열리고, 끄면 이벤트만 발생합니다.
        /// </summary>
        public bool openFiles { get; set; } = true;

        public Toolbar header { get; }

        public ToolbarGroup navigationGroup { get; }

        public Button back { get; }
        public Button forward { get; }
        public Button up { get; }

        public Label pathPrefixLabel { get; }

        public IOBreadcrumbBar breadcrumbBar { get; }

        /// <summary>
        /// 경로를 직접 입력하는 필드입니다. <see cref="breadcrumbBar"/>의 빈 곳을 누르거나 Ctrl+L을 누르면 <see cref="breadcrumbBar"/> 대신 나타납니다.
        /// </summary>
        public TextField pathField { get; }

        public Button folderMenu { get; }

        public TwoPaneSplitView splitView { get; }

        public ScrollView navigationPane { get; }

        /// <summary>
        /// <see cref="SetNavigationGroups"/>로 지정한 네비게이션 영역의 장소 그룹입니다.
        /// </summary>
        public IReadOnlyList<IOBrowserPlaceGroup> navigationGroups => _navigationGroups;
        readonly List<IOBrowserPlaceGroup> _navigationGroups = [];

        public IOBrowserTabView contentPane { get; }

        protected IOBrowser()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));

            AddToClassList(ussClassName);

            // 포커스를 받을 수 없는 자식을 클릭해도 포커스가 브라우저에 남아, 키 입력이 브라우저까지 전달되게 합니다.
            // ReSharper disable once VirtualMemberCallInConstructor
            focusable = true;

            header = new Toolbar { name = "header" };
            header.AddToClassList(headerUssClassName);
            {
                navigationGroup = new ToolbarGroup { name = "navigation-group" };
                navigationGroup.AddToClassList(navigationGroupUssClassName);
                {
                    back = new Button(Back) { name = "back" };
                    back.SetEnabled(false);
                    back.AddToClassList(headerButtonUssClassName);
                    back.AddToClassList(navigationButtonUssClassName);
                    back.AddToClassList(backUssClassName);
                    back.AddManipulator(new AssetScopeManipulator<Texture2D>
                    {
                        assetRef = "runios:ui/io_browser/back",
                        editorAssetRef = "runios-editor:ui/io_browser/back",
                        editorDarkAssetRef = "runios-editor:ui_dark/io_browser/back",
                        applyAsset = x => back.iconImage = x,
                        clearAsset = () => back.iconImage = default
                    });
                    navigationGroup.Add(back);

                    forward = new Button(Forward) { name = "forward" };
                    forward.SetEnabled(false);
                    forward.AddToClassList(headerButtonUssClassName);
                    forward.AddToClassList(navigationButtonUssClassName);
                    forward.AddToClassList(forwardUssClassName);
                    forward.AddManipulator(new AssetScopeManipulator<Texture2D>
                    {
                        assetRef = "runios:ui/io_browser/forward",
                        editorAssetRef = "runios-editor:ui/io_browser/forward",
                        editorDarkAssetRef = "runios-editor:ui_dark/io_browser/forward",
                        applyAsset = x => forward.iconImage = x,
                        clearAsset = () => forward.iconImage = default
                    });
                    navigationGroup.Add(forward);

                    up = new Button(Up) { name = "up" };
                    up.AddToClassList(headerButtonUssClassName);
                    up.AddToClassList(navigationButtonUssClassName);
                    up.AddToClassList(upUssClassName);
                    up.AddManipulator(new AssetScopeManipulator<Texture2D>
                    {
                        assetRef = "runios:ui/io_browser/up",
                        editorAssetRef = "runios-editor:ui/io_browser/up",
                        editorDarkAssetRef = "runios-editor:ui_dark/io_browser/up",
                        applyAsset = x => up.iconImage = x,
                        clearAsset = () => up.iconImage = default
                    });
                    navigationGroup.Add(up);
                }
                header.Add(navigationGroup);

                header.Add(new FlexibleSpace());

                pathPrefixLabel = new Label { name = "path-prefix-label" };
                pathPrefixLabel.AddToClassList(pathPrefixLabelUssClassName);
                header.Add(pathPrefixLabel);

                breadcrumbBar = new IOBreadcrumbBar { name = "breadcrumb-bar" };
                breadcrumbBar.AddToClassList(breadcrumbBarUssClassName);
                breadcrumbBar.nodeSelected += x => node = x;
                breadcrumbBar.editRequested += EditPath;
                header.Add(breadcrumbBar);

                pathField = new TextField { name = "path-field", isDelayed = true };
                pathField.AddToClassList(pathFieldUssClassName);
                pathField.style.display = DisplayStyle.None;
                pathField.RegisterValueChangedCallback(x =>
                {
                    node = PathFieldToNode(x.newValue);
                    EndEditPath();
                });
                pathField.RegisterCallback<FocusOutEvent>(_ => EndEditPath());
                pathField.RegisterCallback<KeyDownEvent>(x =>
                {
                    if (x.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                    {
                        // Enter는 필드가 처리하면서 포커스를 필드 자신으로 옮기므로, 처리가 끝난 뒤 브라우저로 포커스를 되돌립니다.
                        // 값이 바뀌지 않았을 때도 필드가 포커스를 잃어 경로 편집이 끝납니다.
                        schedule.Execute(Focus);
                        return;
                    }

                    if (x.keyCode != KeyCode.Escape)
                        return;

                    EndEditPath();
                    Focus();
                    x.StopPropagation();
                }, TrickleDown.TrickleDown);
                header.Add(pathField);

                folderMenu = new Button(FolderMenuDropDown) { name = "folder-menu" };
                folderMenu.AddToClassList(headerButtonUssClassName);
                folderMenu.AddToClassList(folderMenuButtonUssClassName);
                folderMenu.AddManipulator(new AssetScopeManipulator<Texture2D>
                {
                    assetRef = "runios:ui/io_browser/menu",
                    editorAssetRef = "runios-editor:ui/io_browser/menu",
                    editorDarkAssetRef = "runios-editor:ui_dark/io_browser/menu",
                    applyAsset = x => folderMenu.iconImage = x,
                    clearAsset = () => folderMenu.iconImage = default
                });
                header.Add(folderMenu);
            }
            hierarchy.Add(header);

            hierarchy.Add(new Separator());

            splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            {
                navigationPane = new ScrollView { name = "navigation-pane" };
                navigationPane.AddToClassList(navigationPaneUssClassName);
                splitView.Add(navigationPane);

                contentPane = new IOBrowserTabView { name = "content-pane", reorderable = true };
                contentPane.AddToClassList(contentPaneUssClassName);
                contentPane.activeTabChanged += (_, _) =>
                {
                    UpdateHeader();

                    // 이전 탭이 숨겨지면 그 안의 포커스가 해제되므로, 브라우저로 포커스를 되돌립니다.
                    Focus();
                };
                splitView.Add(contentPane);
            }
            hierarchy.Add(splitView);

            pathField.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());
            breadcrumbBar.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());
            contentPane.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());

            RegisterCallback<KeyDownEvent>(OnKeyDown);

            // 방향키는 KeyDown과 별도로 보내지는 NavigationMove로 트리 뷰가 처리하므로, 트리 뷰에 닿기 전에 가로챕니다.
            RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);
        }

        public void Back() => activeTab.Back();

        public void Forward() => activeTab.Forward();

        public virtual void Up() => node = node.GetParent();

        void OnTabFileOpened(IONode fileNode)
        {
            fileOpened?.Invoke(fileNode);

            if (openFiles && ServiceRegistry.TryGet(out IFileOpenService? fileOpen) && fileNode.provider is IPhysicalBackedIOProvider physicalProvider)
                fileOpen.OpenFile(physicalProvider.GetPhysicalPath(fileNode.path)).Forget();
        }

        /// <summary>
        /// 네비게이션 영역의 장소 그룹을 지정한 그룹들로 교체합니다. 그룹에는 <see cref="readOnly"/>가 적용됩니다.
        /// </summary>
        public void SetNavigationGroups(IEnumerable<IOBrowserPlaceGroup> groups)
        {
            navigationPane.Clear();
            _navigationGroups.Clear();

            foreach (IOBrowserPlaceGroup group in groups)
            {
                group.readOnly = readOnly;

                navigationPane.Add(group);
                _navigationGroups.Add(group);
            }
        }

        /// <summary>
        /// 활성 탭의 내용을 다시 읽어 옵니다.
        /// </summary>
        public UniTask Refresh() => activeTab.Refresh();

        /// <summary>
        /// 지정한 위치를 여는 새 탭을 추가하고 활성화합니다.
        /// </summary>
        public IOBrowserTab AddTab(IONode node)
        {
            IOBrowserTab tab = CreateTab();
            tab.readOnly = readOnly;
            tab.filter = filter;
            tab.name = "tab";
            tab.AddToClassList(tabUssClassName);
            tab.nodeChanged += OnTabNodeChanged;
            tab.fileOpened += OnTabFileOpened;

            // 닫기 버튼도 CloseTab으로 닫히도록, 직접 닫은 뒤 TabView의 기본 닫기 동작은 취소합니다.
            tab.closing += () =>
            {
                CloseTab(tab);
                return false;
            };

            contentPane.Add(tab);
            contentPane.activeTab = tab;

            UpdateTabHeaderDisplay();

            // 생성이 모두 끝난 뒤에 위치를 정해, 탭의 내용 갱신(OnNodeChanged)이 생성자에서 호출되지 않게 합니다.
            tab.ResetNode(node);

            // 이전 탭이 숨겨지면 그 안의 포커스가 해제되므로, 브라우저로 포커스를 되돌립니다.
            Focus();
            return tab;
        }

        /// <summary>
        /// 탭을 닫습니다. 탭이 하나만 남아 있으면 닫지 않습니다.
        /// </summary>
        public void CloseTab(IOBrowserTab tab)
        {
            if (contentPane.childCount <= 1)
                return;

            // TabView는 활성 탭이 제거되면 첫 탭을 활성화하므로, 제거하기 전에 옆 탭을 활성화합니다.
            if (tab == contentPane.activeTab)
            {
                int index = contentPane.selectedTabIndex;
                contentPane.selectedTabIndex = index + 1 < contentPane.childCount ? index + 1 : index - 1;
            }

            tab.RemoveFromHierarchy();
            UpdateTabHeaderDisplay();

            // 닫은 탭 안에 포커스가 있었으면 함께 사라지므로, 브라우저로 포커스를 되돌립니다.
            Focus();
        }

        // 탭이 하나뿐이면 탭 헤더를 숨깁니다.
        void UpdateTabHeaderDisplay() => contentPane.contentViewport.style.display = contentPane.childCount > 1 ? DisplayStyle.Flex : DisplayStyle.None;

        void OnTabNodeChanged(IOBrowserTab tab)
        {
            tab.RefreshLabel();

            if (tab == contentPane.activeTab)
                UpdateHeader();
        }

        void UpdateHeader()
        {
            back.SetEnabled(activeTab.canGoBack);
            forward.SetEnabled(activeTab.canGoForward);

            pathField.SetValueWithoutNotify(NodeToPathField(node));
            breadcrumbBar.SetCrumbs(NodeToBreadcrumbs(node));
        }

        void EditPath()
        {
            breadcrumbBar.style.display = DisplayStyle.None;
            pathField.style.display = DisplayStyle.Flex;

            // 표시가 바뀐 스타일이 적용된 뒤에야 포커스를 받을 수 있습니다.
            pathField.schedule.Execute(() =>
            {
                pathField.Focus();
                pathField.SelectAll();
            }).StartingIn(1);
        }

        void EndEditPath()
        {
            pathField.style.display = DisplayStyle.None;
            breadcrumbBar.style.display = DisplayStyle.Flex;
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.F5 && !evt.actionKey)
                Refresh().Forget();
            else if (evt.keyCode == KeyCode.L && evt.actionKey)
                EditPath();
            else if (evt.keyCode == KeyCode.T && evt.actionKey)
                AddTab(node);
            else if (evt.keyCode == KeyCode.W && evt.actionKey)
                CloseTab(activeTab);
            else if (showHiddenFilesShortcut.Matches(evt))
                ToggleShowHiddenFiles();
            else
                return;

            evt.StopPropagation();
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            if (!evt.altKey)
                return;

            if (evt.direction == NavigationMoveEvent.Direction.Left)
                Back();
            else if (evt.direction == NavigationMoveEvent.Direction.Right)
                Forward();
            else if (evt.direction == NavigationMoveEvent.Direction.Up)
                Up();
            else
                return;

            // 전파를 멈춰도 포커스 이동은 따로 일어나므로, 포커스도 옮기지 않도록 합니다.
            evt.StopPropagation();
            focusController?.IgnoreEvent(evt);
        }

        public void AlignPathField()
        {
            VisualElement active = pathField.style.display == DisplayStyle.None ? breadcrumbBar : pathField;

            float xMax = active.layout.xMax;
            float x = contentPane.ChangeCoordinatesTo(header, Vector2.zero).x;

            float width = (xMax - x).Clamp(0);
            pathField.style.width = width;
            breadcrumbBar.style.width = width;
        }

        RuniMenu? folderDropdown;

        protected void FolderMenuDropDown()
        {
            if (folderDropdown == null)
            {
                folderDropdown = new RuniMenu();
                BuildFolderMenu(folderDropdown);
            }

            folderDropdown.DropDown(folderMenu.worldBound, folderMenu);
        }

        static readonly KeyShortcut newTabShortcut = new(KeyCode.T, actionKey: true);
        static readonly KeyShortcut closeTabShortcut = new(KeyCode.W, actionKey: true);
        static readonly KeyShortcut refreshShortcut = new(KeyCode.F5);
        protected static readonly KeyShortcut showHiddenFilesShortcut = new(KeyCode.H, actionKey: true);

        protected virtual void ToggleShowHiddenFiles() { }

        /// <summary>
        /// 폴더 메뉴를 처음 열 때 한 번 구성합니다. 열 때마다 바뀌는 상태는 <see cref="RuniMenu.opened"/>에서 갱신합니다.
        /// </summary>
        protected virtual void BuildFolderMenu(RuniMenu menu)
        {
            const string newTabKey = "runios-editor:io_browser.new_tab";
            RuniMenuItem newTab = new(string.Empty, () => AddTab(node)) { shortcuts = [newTabShortcut] };
            menu.opened += () => newTab.text = LocalizationUtility.GetText(newTabKey) ?? newTabKey;
            menu.Add(newTab);

            const string closeTabKey = "runios-editor:io_browser.close_tab";
            RuniMenuItem closeTab = new(string.Empty, () => CloseTab(activeTab)) { shortcuts = [closeTabShortcut] };
            menu.opened += () =>
            {
                closeTab.text = LocalizationUtility.GetText(closeTabKey) ?? closeTabKey;

                // 마지막 탭은 닫히지 않습니다.
                closeTab.SetEnabled(contentPane.childCount > 1);
            };

            menu.Add(closeTab);

            menu.Add(new Separator());

            const string refreshKey = "runios-editor:io_browser.refresh";
            RuniMenuItem refresh = new(string.Empty, () => Refresh().Forget()) { shortcuts = [refreshShortcut] };
            menu.opened += () => refresh.text = LocalizationUtility.GetText(refreshKey) ?? refreshKey;
            menu.Add(refresh);

            menu.Add(new Separator());

            const string openInFileManagerKey = "runios-editor:io_browser.open_in_file_manager";
            RuniMenuItem openInFileManager = new(string.Empty, () =>
            {
                if (ServiceRegistry.TryGet(out IFileManagerService? fileManager) && node.provider is IPhysicalBackedIOProvider physicalProvider)
                    fileManager.ShowFolder(physicalProvider.GetPhysicalPath(node.path)).Forget();
            });

            menu.opened += () =>
            {
                openInFileManager.text = LocalizationUtility.GetText(openInFileManagerKey) ?? openInFileManagerKey;

                // 실제 경로가 있는 노드이고 이 플랫폼에 파일 관리자 연동이 있을 때만 열 수 있습니다.
                openInFileManager.SetEnabled(ServiceRegistry.Get<IFileManagerService>() != null && node.provider is IPhysicalBackedIOProvider);
            };

            menu.Add(openInFileManager);
        }

        /// <summary>
        /// 이 브라우저의 빈 탭을 만듭니다. 위치는 <see cref="AddTab"/>이 탭을 추가한 뒤 정합니다.
        /// </summary>
        protected abstract IOBrowserTab CreateTab();

        protected virtual string NodeToPathField(IONode node) => node.path.value;

        /// <summary>
        /// 경로 표시줄의 버튼으로 쓸 이름과 노드를 앞에서부터 순서대로 가져옵니다.
        /// </summary>
        protected virtual IEnumerable<(string name, IONode node)> NodeToBreadcrumbs(IONode node)
        {
            IONode current = node.rootNode;
            yield return (RuniPath.directorySeparatorChar.ToString(), current);

            foreach (string segment in node.path.GetSegments())
            {
                current = current.CreateChild(segment);
                yield return (segment, current);
            }
        }

        protected virtual IONode PathFieldToNode(string path) => new IONode(provider, (RuniPath)path);
    }
}