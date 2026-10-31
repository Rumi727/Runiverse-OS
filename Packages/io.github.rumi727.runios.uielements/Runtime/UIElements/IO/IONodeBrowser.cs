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

                activeTab.ResetNode(new PhysicalIOProvider(value.Value).rootNode);
            }
        }

        /// <summary>
        /// 활성 탭의 트리 뷰입니다.
        /// </summary>
        public IONodeMultiColumnTreeView treeView => ((IONodeBrowserTab)activeTab).treeView;

        /// <summary>
        /// 모든 탭의 트리 뷰에 적용되고, 새로 여는 탭에도 적용됩니다.
        /// </summary>
        [UxmlAttribute]
        public SelectionType selectionType
        {
            get;
            set
            {
                field = value;
                foreach (IOBrowserTab tab in tabs)
                    ((IONodeBrowserTab)tab).selectionType = value;
            }
        } = SelectionType.Multiple;

        public IONodeBrowser() : this(IONode.empty) { }

        public IONodeBrowser(IIOProvider provider) : this(provider.rootNode) { }

        public IONodeBrowser(IONode node) => AddTab(node);

        protected override IOBrowserTab CreateTab() => new IONodeBrowserTab();

        protected override void BuildFolderMenu(RuniMenu menu)
        {
            base.BuildFolderMenu(menu);

            RuniMenuItem showHiddenFiles = new(string.Empty, () => treeView.showHiddenFiles = !treeView.showHiddenFiles);
            menu.opened += () =>
            {
                showHiddenFiles.text = LocalizationUtility.GetText("runios-editor:io_browser.show_hidden_files") ?? "runios-editor:io_browser.show_hidden_files";
                showHiddenFiles.isChecked = treeView.showHiddenFiles;
            };

            menu.Add(showHiddenFiles);
        }

        public UniTask Refresh() => ((IONodeBrowserTab)activeTab).Refresh();
    }
}
