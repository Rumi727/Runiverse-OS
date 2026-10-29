#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IONodeBrowser : IOBrowser
    {
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
            }
        }

        public IONodeMultiColumnTreeView treeView { get; }

        [UxmlAttribute]
        public SelectionType selectionType
        {
            get => treeView.selectionType;
            set => treeView.selectionType = value;
        }

        public IONodeBrowser() : this(IONode.empty) { }

        public IONodeBrowser(IIOProvider provider) : this(provider.rootNode) { }

        public IONodeBrowser(IONode node)
        {
            treeView = new IONodeMultiColumnTreeView { name = "tree-view" };
            treeView.opened += OpenEntry;
            contentPane.Add(treeView);

            this.node = node;
        }

        protected override void OnNodeChanged()
        {
            treeView.node = node;
            Refresh().Forget();
        }

        public UniTask Refresh() => treeView.Refresh();
    }
}
