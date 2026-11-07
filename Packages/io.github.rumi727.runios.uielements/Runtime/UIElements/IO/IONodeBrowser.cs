#nullable enable
using RuniOS.IO;
using System.Collections.Immutable;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IONodeBrowser : IOBrowser
    {
        public new const string ussClassName = "runios-io-node-browser";

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

        /// <summary>
        /// 어느 탭에서든 선택이 바뀌기 직전에 그 탭과 새 선택을 묻습니다. 구독자가 하나라도 <see langword="false"/>를 반환하면 그 탭의 선택을 되돌립니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.selectionChanging"/>
        public event Func<IONodeBrowserTab, ImmutableArray<IOEntry>, bool>? selectionChanging;

        /// <summary>
        /// 어느 탭에서든 선택이 바뀐 뒤에 그 탭과 새 선택을 알립니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.selectionChanged"/>
        public event Action<IONodeBrowserTab, ImmutableArray<IOEntry>>? selectionChanged;

        public IONodeBrowser() : this(IONode.empty) { }

        public IONodeBrowser(IIOProvider provider) : this(provider.rootNode) { }

        public IONodeBrowser(IONode node)
        {
            AddToClassList(ussClassName);
            AddTab(node);
        }

        protected override IOBrowserTab CreateTab()
        {
            IONodeBrowserTab tab = new IONodeBrowserTab { selectionType = selectionType };

            IONodeMultiColumnTreeView tabTreeView = tab.treeView;
            tabTreeView.selectionChanging += x => selectionChanging?.GetInvocationList().OfType<Func<IONodeBrowserTab, ImmutableArray<IOEntry>, bool>>().All(y => y.Invoke(tab, x)) ?? true;
            tabTreeView.selectionChanged += x => selectionChanged?.Invoke(tab, x);

            return tab;
        }

        protected override void ToggleShowHiddenFiles() => treeView.showHiddenFiles = !treeView.showHiddenFiles;

        protected override void BuildFolderMenu(RuniMenu menu)
        {
            IONodeMultiColumnTreeView.AddNewItemMenu(menu, () => treeView);
            menu.Add(new Separator());

            base.BuildFolderMenu(menu);

            RuniMenuItem showHiddenFiles = new(string.Empty, ToggleShowHiddenFiles) { shortcuts = [showHiddenFilesShortcut] };
            menu.opened += () =>
            {
                showHiddenFiles.text = LocalizationUtility.GetText("runios-editor:io_browser.show_hidden_files") ?? "runios-editor:io_browser.show_hidden_files";
                showHiddenFiles.isChecked = treeView.showHiddenFiles;
            };

            menu.Add(showHiddenFiles);
        }
    }
}
