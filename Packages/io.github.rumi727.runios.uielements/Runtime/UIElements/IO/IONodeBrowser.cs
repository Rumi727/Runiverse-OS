#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IONodeBrowser : VisualElement
    {
        public const string ussClassName = "runios-io-node-browser";
        public const string headerUssClassName = "runios-io-node-browser__header";

        public const string headerButtonUssClassName = "runios-io-node-browser__header-button";

        public const string navigationGroupUssClassName = "runios-io-node-browser__navigation-group";
        public const string navigationButtonUssClassName = "runios-io-node-browser__navigation-button";

        public const string backUssClassName = "runios-io-node-browser__back-button";
        public const string forwardUssClassName = "runios-io-node-browser__forward-button";
        public const string upUssClassName = "runios-io-node-browser__up-button";

        public const string pathPrefixLabelUssClassName = "runios-io-node-browser__path-prefix-label";
        public const string pathFieldUssClassName = "runios-io-node-browser__path-field";

        public const string folderMenuButtonUssClassName = "runios-io-node-browser__folder-menu";

        public const string navigationPaneUssClassName = "runios-io-node-browser__navigation-pane";
        public const string contentPaneUssClassName = "runios-io-node-browser__content-pane";

        public IIOProvider provider => node.provider;
        public IONode node
        {
            get;
            set
            {
                field = value;
                contentPane.node = value;
            }
        }

        [UxmlAttribute]
        SerializableNullable<PhysicalPath> targetPath
        {
            get
            {
                if (node.provider is PhysicalIOProvider provider)
                    return provider.targetPath;

                return null;
            }
            set
            {
                if (value == null || targetPath == value)
                    return;

                node = new PhysicalIOProvider(value.Value).rootNode;
                Refresh().Forget();
            }
        }

        public override VisualElement contentContainer => navigationPane;

        public Toolbar header { get; }

        public ToolbarGroup navigationGroup { get; }

        public Button back { get; }
        public Button forward { get; }
        public Button up { get; }

        public Label pathPrefixLabel { get; }
        public RuniPathField pathField { get; }

        public Button folderMenu { get; }
        protected GenericDropdownMenu folderMenuDropdown { get; }

        public TwoPaneSplitView splitView { get; }

        public ScrollView navigationPane { get; }

        public IONodeMultiColumnTreeView contentPane { get; }

        public IONodeBrowser() : this(IONode.empty) { }

        public IONodeBrowser(IIOProvider provider) : this(provider.rootNode) { }

        public IONodeBrowser(IONode node)
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
                        assetRef = "runios:ui/io_node_browser/back",
                        editorAssetRef = "runios-editor:ui/io_node_browser/back",
                        editorDarkAssetRef = "runios-editor:ui_dark/io_node_browser/back",
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
                        assetRef = "runios:ui/io_node_browser/forward",
                        editorAssetRef = "runios-editor:ui/io_node_browser/forward",
                        editorDarkAssetRef = "runios-editor:ui_dark/io_node_browser/forward",
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
                        assetRef = "runios:ui/io_node_browser/up",
                        editorAssetRef = "runios-editor:ui/io_node_browser/up",
                        editorDarkAssetRef = "runios-editor:ui_dark/io_node_browser/up",
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

                pathField = new RuniPathField { name = "path-field", isDelayed = true };
                pathField.AddToClassList(pathFieldUssClassName);
                header.Add(pathField);

                folderMenu = new Button(FolderMenuDropDown) { name = "folder-menu" };
                folderMenu.AddToClassList(headerButtonUssClassName);
                folderMenu.AddToClassList(folderMenuButtonUssClassName);
                folderMenu.AddManipulator(new AssetScopeManipulator<Texture2D>
                {
                    assetRef = "runios:ui/io_node_browser/menu",
                    editorAssetRef = "runios-editor:ui/io_node_browser/menu",
                    editorDarkAssetRef = "runios-editor:ui_dark/io_node_browser/menu",
                    applyAsset = x => folderMenu.iconImage = x,
                    clearAsset = () => folderMenu.iconImage = default
                });
                folderMenuDropdown = new GenericDropdownMenu();
                folderMenuDropdown.AddItem("Open System Explorer", false, () => { });
                header.Add(folderMenu);
            }
            hierarchy.Add(header);

            hierarchy.Add(new Separator());

            splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            {
                navigationPane = new ScrollView { name = "navigation-pane" };
                navigationPane.AddToClassList(navigationPaneUssClassName);
                splitView.Add(navigationPane);

                contentPane = new IONodeMultiColumnTreeView { name = "content-pane" };
                contentPane.AddToClassList(contentPaneUssClassName);

                splitView.Add(contentPane);
            }
            hierarchy.Add(splitView);

            pathField.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());
            contentPane.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());

            this.node = node;
        }

        public void Back()
        {

        }

        public void Forward()
        {

        }

        public void Up() => node = node.GetParent();

        public void AlignPathField()
        {
            float xMax = pathField.layout.xMax;
            float x = contentPane.ChangeCoordinatesTo(header, Vector2.zero).x;

            pathField.style.width = (xMax - x).Clamp(0);
        }

        protected void FolderMenuDropDown() => folderMenuDropdown.DropDown(folderMenu.worldBound, folderMenu, DropdownMenuSizeMode.Auto);

        public UniTask Refresh() => contentPane.Refresh();
    }
}