#nullable enable
using Cysharp.Threading.Tasks;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IONodeBrowserTab : IOBrowserTab
    {
        public IONodeMultiColumnTreeView treeView { get; }

        [UxmlAttribute]
        public SelectionType selectionType
        {
            get;
            set
            {
                field = value;
                treeView.selectionType = value;
            }
        } = SelectionType.Multiple;

        public IONodeBrowserTab()
        {
            treeView = new IONodeMultiColumnTreeView { name = "tree-view" };
            treeView.opened += OpenEntry;

            Add(treeView);
        }

        protected override void OnNodeChanged()
        {
            treeView.node = node;
            Refresh().Forget();
        }

        public UniTask Refresh() => treeView.Refresh();
    }
}
