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

        /// <summary>
        /// 어느 탭에서든 그 탭에서 시작한 이동 직전에 탭과 옮길 항목을 묻습니다. 구독자가 하나라도 <see langword="false"/>를 반환하면 옮기지 않습니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.moving"/>
        public event Func<IONodeBrowserTab, ImmutableArray<IONode>, bool>? moving;

        /// <summary>
        /// 어느 탭에서든 그 탭에서 시작한 이동으로 옮겨진 항목을 탭과 함께 알립니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.moved"/>
        public event Action<IONodeBrowserTab, ImmutableArray<(IONode source, IONode destination)>>? moved;

        /// <summary>
        /// 어느 탭에서든 그 탭에서 시작한 삭제나 덮어쓰기 직전에 탭과 사라질 항목을 묻습니다. 구독자가 하나라도 <see langword="false"/>를 반환하면 지우거나 덮어쓰지 않습니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.deleting"/>
        public event Func<IONodeBrowserTab, ImmutableArray<IONode>, bool>? deleting;

        /// <summary>
        /// 어느 탭에서든 그 탭에서 시작한 삭제나 덮어쓰기로 사라지거나 대체된 항목을 탭과 함께 알립니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.deleted"/>
        public event Action<IONodeBrowserTab, ImmutableArray<IONode>>? deleted;

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
            tabTreeView.moving += x => moving?.GetInvocationList().OfType<Func<IONodeBrowserTab, ImmutableArray<IONode>, bool>>().All(y => y.Invoke(tab, x)) ?? true;
            tabTreeView.moved += x => moved?.Invoke(tab, x);
            tabTreeView.deleting += x => deleting?.GetInvocationList().OfType<Func<IONodeBrowserTab, ImmutableArray<IONode>, bool>>().All(y => y.Invoke(tab, x)) ?? true;
            tabTreeView.deleted += x => deleted?.Invoke(tab, x);

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
