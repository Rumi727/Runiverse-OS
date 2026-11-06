#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IONodeBrowserTab : IOBrowserTab
    {
        public new const string ussClassName = "runios-io-node-browser-tab";

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

        public override bool readOnly
        {
            get => treeView.readOnly;
            set => treeView.readOnly = value;
        }

        public override IPatternMatcher? filter
        {
            get => treeView.filter;
            set => treeView.filter = value;
        }

        public IONodeBrowserTab()
        {
            AddToClassList(ussClassName);

            treeView = new IONodeMultiColumnTreeView { name = "tree-view", contextMenuEnabled = true };
            treeView.opened += OpenEntry;

            Add(treeView);
        }

        protected override void OnNodeChanged()
        {
            treeView.node = node;
            Refresh().Forget();
        }

        public override UniTask Refresh() => treeView.Refresh();
    }
}
