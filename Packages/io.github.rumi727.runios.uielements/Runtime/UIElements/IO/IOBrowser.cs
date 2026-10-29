#nullable enable
using RuniOS.IO;
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

        public IIOProvider provider => node.provider;
        public IONode node
        {
            get;
            set
            {
                field = value;

                pathField.SetValueWithoutNotify(ToPath(value));
                OnNodeChanged();
            }
        } = IONode.empty;

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

        public VisualElement contentPane { get; }

        protected IOBrowser()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));

            AddToClassList(ussClassName);

            header = new Toolbar { name = "header" };
            header.AddToClassList(headerUssClassName);
            {
                navigationGroup = new ToolbarGroup { name = "navigation-group" };
                navigationGroup.AddToClassList(navigationGroupUssClassName);
                {
                    back = new Button(Back) { name = "back" };
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
                pathField.RegisterValueChangedCallback(x => node = ToNode(x.newValue));
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

                contentPane = new VisualElement { name = "content-pane" };
                contentPane.AddToClassList(contentPaneUssClassName);

                splitView.Add(contentPane);
            }
            hierarchy.Add(splitView);

            pathField.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());
            contentPane.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());
        }

        public void Back()
        {

        }

        public void Forward()
        {

        }

        public virtual void Up() => node = node.GetParent();

        /// <summary>
        /// 폴더는 그 폴더로 이동하고, 파일은 <see cref="fileOpened"/>를 발생시킵니다.
        /// </summary>
        protected void OpenEntry(IOEntry entry)
        {
            IONode entryNode = node.Bind(entry);

            if (entry.isDirectory)
                node = entryNode;
            else
                fileOpened?.Invoke(entryNode);
        }

        public void AlignPathField()
        {
            float xMax = pathField.layout.xMax;
            float x = contentPane.ChangeCoordinatesTo(header, Vector2.zero).x;

            pathField.style.width = (xMax - x).Clamp(0);
        }

        // 체크 상태 같은 항목이 현재 상태를 반영하도록, 열 때마다 메뉴를 새로 구성합니다.
        protected void FolderMenuDropDown()
        {
            GenericDropdownMenu menu = new();
            BuildFolderMenu(menu);
            menu.DropDown(folderMenu.worldBound, folderMenu, DropdownMenuSizeMode.Auto);
        }

        protected virtual void BuildFolderMenu(GenericDropdownMenu menu) => menu.AddItem("Open System Explorer", false, () => { });

        protected abstract void OnNodeChanged();

        protected virtual string ToPath(IONode node) => node.path.value;

        protected virtual IONode ToNode(string path) => new IONode(provider, (RuniPath)path);
    }
}