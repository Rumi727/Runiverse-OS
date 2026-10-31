#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.IO.Locations;
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

        public IOBrowserTab activeTab => (IOBrowserTab)contentPane.activeTab;
        public IEnumerable<IOBrowserTab> tabs => contentPane.Children().Cast<IOBrowserTab>();

        /// <summary>
        /// 내용 영역에서 파일을 열면 발생합니다. 파일을 여는 동작은 구독자가 정합니다.
        /// </summary>
        public event Action<IONode>? fileOpened;

        public Toolbar header { get; }

        public ToolbarGroup navigationGroup { get; }

        public Button back { get; }
        public Button forward { get; }
        public Button up { get; }

        public Label pathPrefixLabel { get; }
        public TextField pathField { get; }

        public Button folderMenu { get; }

        public TwoPaneSplitView splitView { get; }

        public ScrollView navigationPane { get; }

        public TabView contentPane { get; }

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

                pathField = new TextField { name = "path-field", isDelayed = true };
                pathField.AddToClassList(pathFieldUssClassName);
                pathField.RegisterValueChangedCallback(x => node = PathFieldToNode(x.newValue));
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

                contentPane = new TabView { name = "content-pane", reorderable = true };
                contentPane.AddToClassList(contentPaneUssClassName);
                contentPane.activeTabChanged += (_, _) => UpdateHeader();
                splitView.Add(contentPane);
            }
            hierarchy.Add(splitView);

            pathField.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());
            contentPane.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());

            RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        public void Back() => activeTab.Back();

        public void Forward() => activeTab.Forward();

        public virtual void Up() => node = node.GetParent();

        /// <summary>
        /// 지정한 위치를 여는 새 탭을 추가하고 활성화합니다.
        /// </summary>
        public IOBrowserTab AddTab(IONode node)
        {
            IOBrowserTab tab = CreateTab();
            tab.name = "tab";
            tab.AddToClassList(tabUssClassName);
            tab.nodeChanged += OnTabNodeChanged;
            tab.fileOpened += x => fileOpened?.Invoke(x);

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
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (!evt.actionKey)
                return;

            if (evt.keyCode == KeyCode.T)
                AddTab(node);
            else if (evt.keyCode == KeyCode.W)
                CloseTab(activeTab);
            else
                return;

            evt.StopPropagation();
        }

        public void AlignPathField()
        {
            float xMax = pathField.layout.xMax;
            float x = contentPane.ChangeCoordinatesTo(header, Vector2.zero).x;

            pathField.style.width = (xMax - x).Clamp(0);
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

        /// <summary>
        /// 폴더 메뉴를 처음 열 때 한 번 구성합니다. 열 때마다 바뀌는 상태는 <see cref="RuniMenu.opened"/>에서 갱신합니다.
        /// </summary>
        protected virtual void BuildFolderMenu(RuniMenu menu)
        {
            const string newTabKey = "runios-editor:io_browser.new_tab";
            RuniMenuItem newTab = new(string.Empty, () => AddTab(node)) { shortcuts = [newTabShortcut] };
            menu.opened += () => newTab.text = LocalizationUtility.GetText(newTabKey) ?? newTabKey;
            menu.Add(newTab);

            menu.Add(new Separator());

            const string openInFileManagerKey = "runios-editor:io_browser.open_in_file_manager";
            RuniMenuItem openInFileManager = new(string.Empty, () =>
            {
                if (IFileManagerProvider.instance is { } fileManager && node.provider is PhysicalIOProvider physicalProvider)
                    fileManager.ShowFolder(physicalProvider.targetPath.Combine(node.path)).Forget();
            });

            menu.opened += () =>
            {
                openInFileManager.text = LocalizationUtility.GetText(openInFileManagerKey) ?? openInFileManagerKey;

                // 실제 경로가 있는 노드이고 이 플랫폼에 파일 관리자 연동이 있을 때만 열 수 있습니다.
                openInFileManager.SetEnabled(IFileManagerProvider.instance != null && node.provider is PhysicalIOProvider);
            };

            menu.Add(openInFileManager);
        }

        /// <summary>
        /// 이 브라우저의 빈 탭을 만듭니다. 위치는 <see cref="AddTab"/>이 탭을 추가한 뒤 정합니다.
        /// </summary>
        protected abstract IOBrowserTab CreateTab();

        protected virtual string NodeToPathField(IONode node) => node.path.value;

        protected virtual IONode PathFieldToNode(string path) => new IONode(provider, (RuniPath)path);
    }
}