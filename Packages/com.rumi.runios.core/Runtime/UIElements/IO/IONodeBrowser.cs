#nullable enable
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

        public const string navigationGroupUssClassName = "runios-io-node-browser__navigation-group";
        public const string navigationButtonUssClassName = "runios-io-node-browser__navigation-button";

        public const string backUssClassName = "runios-io-node-browser__back-button";
        public const string forwardUssClassName = "runios-io-node-browser__forward-button";
        public const string upUssClassName = "runios-io-node-browser__up-button";

        public const string pathFieldUssClassName = "runios-io-node-browser__path-field";

        public const string navigationPaneUssClassName = "runios-io-node-browser__navigation-pane";
        public const string contentPaneUssClassName = "runios-io-node-browser__content-pane";

        public IONode node { get; set; }

        public Toolbar header { get; }

        public ToolbarGroup navigationGroup { get; }

        public Button back { get; }
        public Button forward { get; }
        public Button up { get; }

        public RuniPathField pathField { get; }

        public TwoPaneSplitView splitView { get; }

        public VisualElement navigationPane { get; }
        public VisualElement contentPane { get; }

        public IONodeBrowser() : this(IONode.empty) { }

        public IONodeBrowser(IONode node)
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            this.node = node;

            AddToClassList(ussClassName);

            header = new Toolbar { name = "header" };
            header.AddToClassList(headerUssClassName);
            {
                navigationGroup = new ToolbarGroup { name = "navigation-group" };
                navigationGroup.AddToClassList(navigationGroupUssClassName);
                {
                    back = new Button { name = "back" };
                    back.AddToClassList(navigationButtonUssClassName);
                    back.AddToClassList(backUssClassName);
                    back.AddManipulator(new AssetScopeManipulator<Texture2D>
                    {
                        assetRef = "runios:ui/io_node_browser/back",
                        editorAssetRef = "runios-editor:ui/io_node_browser/back",
                        applyAsset = x => back.iconImage = x,
                        clearAsset = () => back.iconImage = default
                    });
                    navigationGroup.Add(back);

                    forward = new Button { name = "forward" };
                    forward.AddToClassList(navigationButtonUssClassName);
                    forward.AddToClassList(forwardUssClassName);
                    forward.AddManipulator(new AssetScopeManipulator<Texture2D>
                    {
                        assetRef = "runios:ui/io_node_browser/forward",
                        editorAssetRef = "runios-editor:ui/io_node_browser/forward",
                        applyAsset = x => back.iconImage = x,
                        clearAsset = () => back.iconImage = default
                    });
                    navigationGroup.Add(forward);

                    up = new Button { name = "up" };
                    up.AddToClassList(navigationButtonUssClassName);
                    up.AddToClassList(upUssClassName);
                    up.AddManipulator(new AssetScopeManipulator<Texture2D>
                    {
                        assetRef = "runios:ui/io_node_browser/up",
                        editorAssetRef = "runios-editor:ui/io_node_browser/up",
                        applyAsset = x => back.iconImage = x,
                        clearAsset = () => back.iconImage = default
                    });
                    navigationGroup.Add(up);
                }
                header.Add(navigationGroup);

                header.Add(new FlexibleSpace());

                pathField = new RuniPathField { name = "path-field", isDelayed = true };
                pathField.AddToClassList(pathFieldUssClassName);
                header.Add(pathField);
            }
            Add(header);

            Add(new Separator());

            splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            {
                navigationPane = new VisualElement { name = "navigation-pane" };
                navigationPane.AddToClassList(navigationPaneUssClassName);
                splitView.Add(navigationPane);

                contentPane = new VisualElement { name = "content-pane" };
                contentPane.AddToClassList(contentPaneUssClassName);
                splitView.Add(contentPane);
            }
            Add(splitView);

            contentPane.RegisterCallback<GeometryChangedEvent>(_ => AlignPathField());
        }

        public void AlignPathField()
        {
            float x = contentPane.ChangeCoordinatesTo(header, Vector2.zero).x;
            float margin = pathField.resolvedStyle.marginLeft + pathField.resolvedStyle.marginRight;

            pathField.style.width = (header.contentRect.xMax - x - margin).Clamp(0);
        }
    }
}