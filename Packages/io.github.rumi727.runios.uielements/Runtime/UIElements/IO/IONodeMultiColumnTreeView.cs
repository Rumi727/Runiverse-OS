#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using RuniOS.IO.Locations;
using RuniOS.Resource;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IONodeMultiColumnTreeView : VisualElement
    {
        public const string ussClassName = "runios-io-node-tree-view";
        public const string cellUssClassName = "runios-io-node-tree-view__cell";
        public const string hiddenCellUssClassName = "runios-io-node-tree-view__cell--hidden";
        public const string nameCellUssClassName = "runios-io-node-tree-view__name-cell";
        public const string nameCellLoadingUssClassName = "runios-io-node-tree-view__name-cell--loading";
        public const string iconUssClassName = "runios-io-node-tree-view__icon";
        public const string nameLabelUssClassName = "runios-io-node-tree-view__name-label";
        public const string loadingImageUssClassName = "runios-io-node-tree-view__loading-image";

        static readonly AssetRef<VectorImage> folderIcon = "runios-ui:ui/io_browser/places/16/folder";
        static readonly AssetRef<VectorImage> folderEditorDarkIcon = "runios-ui:ui_dark/io_browser/places/16/folder";
        static readonly AssetRef<VectorImage> unknownIcon = "runios-ui:ui/io_browser/generic/16/unknown";
        static readonly AssetRef<VectorImage> unknownEditorDarkIcon = "runios-ui:ui_dark/io_browser/generic/16/unknown";
        static readonly Identifier extensionIconDirectory = "runios-ui:ui/io_browser/extensions/16";
        static readonly Identifier extensionEditorDarkIconDirectory = "runios-ui:ui_dark/io_browser/extensions/16";

        public IIOProvider provider => node.provider;
        public IONode node { get; set; }

        /// <summary>
        /// 숨김 파일과 폴더를 표시할지 정합니다. 바꾸면 목록을 다시 불러옵니다.
        /// </summary>
        [UxmlAttribute]
        public bool showHiddenFiles
        {
            get;
            set
            {
                if (field == value)
                    return;

                field = value;
                Refresh().Forget();
            }
        }

        [UxmlAttribute]
        public SelectionType selectionType
        {
            get => treeView.selectionType;
            set => treeView.selectionType = value;
        }

        /// <summary>
        /// 선택된 항목을 가져옵니다.
        /// </summary>
        // DefaultMultiColumnTreeViewController.GetItemForIndex가 정렬을 반영하지 않아 selectedItems가 화면과 어긋나므로, 선택된 id로 항목을 찾습니다.
        public IOEntry[] selectedEntries => treeView.selectedIds.Select(treeView.GetItemDataForId<IOEntry>).ToArray();

        /// <summary>
        /// 항목을 더블 클릭하거나 Enter 키로 고르면 발생합니다.
        /// </summary>
        public event Action<IOEntry>? opened;

        protected MultiColumnTreeView treeView { get; }
        protected LazyTreeLoader<IOEntry> loader { get; }

        protected Column nameColumn { get; }
        protected Column sizeColumn { get; }
        protected Column creationTimeColumn { get; }
        protected Column lastAccessTime { get; }
        protected Column lastWriteTimeColumn { get; }

        public IONodeMultiColumnTreeView() : this(IONode.empty) { }

        public IONodeMultiColumnTreeView(IONode node)
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));

            AddToClassList(ussClassName);

            nameColumn = new Column { name = "name", stretchable = true, optional = false };
            nameColumn.makeCell += MakeNameCell;
            nameColumn.bindCell += BindNameCell;
            nameColumn.comparison = CompareByIndex(nameColumn, (x, y) => string.Compare(x.metaData.name, y.metaData.name, StringComparison.CurrentCultureIgnoreCase));

            sizeColumn = new Column { name = "size", width = 80 };
            sizeColumn.makeCell += MakeCell<Label>("size");
            sizeColumn.bindCell += BindSizeCell;
            sizeColumn.comparison = CompareByIndex(sizeColumn, (x, y) => Nullable.Compare(x.metaData.size, y.metaData.size));

            creationTimeColumn = new Column { name = "creationTime", visible = false, width = 200 };
            creationTimeColumn.makeCell += MakeCell<Label>("creation-time");
            creationTimeColumn.bindCell += BindCell<Label>((element, entry) => element.text = entry.metaData.creationTime.ToString());
            creationTimeColumn.comparison = CompareByIndex(creationTimeColumn, (x, y) => Nullable.Compare(x.metaData.creationTime, y.metaData.creationTime));

            lastAccessTime = new Column { name = "lastAccessTime", visible = false, width = 200 };
            lastAccessTime.makeCell += MakeCell<Label>("last-access-time");
            lastAccessTime.bindCell += BindCell<Label>((element, entry) => element.text = entry.metaData.lastAccessTime.ToString());
            lastAccessTime.comparison = CompareByIndex(lastAccessTime, (x, y) => Nullable.Compare(x.metaData.lastAccessTime, y.metaData.lastAccessTime));

            lastWriteTimeColumn = new Column { name = "lastWriteTime", width = 200 };
            lastWriteTimeColumn.makeCell += MakeCell<Label>("last-write-time");
            lastWriteTimeColumn.bindCell += BindCell<Label>((element, entry) => element.text = entry.metaData.lastWriteTime.ToString());
            lastWriteTimeColumn.comparison = CompareByIndex(lastWriteTimeColumn, (x, y) => Nullable.Compare(x.metaData.lastWriteTime, y.metaData.lastWriteTime));

            treeView = new MultiColumnTreeView([nameColumn, sizeColumn, creationTimeColumn, lastAccessTime, lastWriteTimeColumn]) { sortingMode = ColumnSortingMode.Default, selectionType = SelectionType.Multiple };
            treeView.sortColumnDescriptions.Add(new SortColumnDescription(nameColumn.name, SortDirection.Ascending));

            loader = new LazyTreeLoader<IOEntry>(treeView, x => x.isDirectory, x => EnumerateEntries(x.path)) { preloadDepth = 1 };
            treeView.SetViewController(new LazyMultiColumnTreeViewController<IOEntry>(treeView, loader));
            treeView.itemsChosen += _ =>
            {
                // opened 구독자가 node를 바꾸면 그 자리에서 트리가 다시 로드되어 이전 id로는 항목을 찾을 수 없으므로, 열기 전에 항목을 모두 가져옵니다.
                foreach (IOEntry entry in selectedEntries)
                    opened?.Invoke(entry);
            };

            // 오른쪽 버튼을 누를 때 컬렉션 뷰가 이미 선택을 바꾸므로, 뗄 때 선택된 항목으로 메뉴를 엽니다.
            RegisterCallback<PointerUpEvent>(e =>
            {
                if (e.button == (int)MouseButton.RightMouse)
                    ShowContextMenu(e.position);
            });

            Add(treeView);

            this.AddManipulator(new LocalizationManipulator(OnLanguageChanged));

            this.node = node;
        }

        void OnLanguageChanged()
        {
            nameColumn.title = GetText("runios-editor:io_node_tree_view.name");
            sizeColumn.title = GetText("runios-editor:io_node_tree_view.size");
            creationTimeColumn.title = GetText("runios-editor:io_node_tree_view.creation_time");
            lastAccessTime.title = GetText("runios-editor:io_node_tree_view.last_access_time");
            lastWriteTimeColumn.title = GetText("runios-editor:io_node_tree_view.last_write_time");

            // 항목 개수 문자열도 언어에 따라 바뀌므로 다시 바인딩합니다.
            treeView.RefreshItems();
        }

        static string GetText(string key) => LocalizationUtility.GetText(key) ?? key;

        static VisualElement MakeNameCell()
        {
            VisualElement cell = new() { name = "name" };
            cell.AddToClassList(cellUssClassName);
            cell.AddToClassList(nameCellUssClassName);

            Image icon = new() { name = "icon" };
            icon.AddToClassList(iconUssClassName);
            AssetScopeManipulator<VectorImage> iconManipulator = new()
            {
                applyAsset = x => icon.vectorImage = x,
                clearAsset = () => icon.vectorImage = null
            };
            icon.AddManipulator(iconManipulator);
            // 바인딩할 때 아이콘을 바꿀 수 있도록 매니퓰레이터를 userData에 둡니다.
            icon.userData = iconManipulator;
            cell.Add(icon);

            Label label = new() { name = "name-label" };
            label.AddToClassList(nameLabelUssClassName);
            cell.Add(label);

            Image loadingImage = new() { name = "loading-image" };
            loadingImage.AddToClassList(loadingImageUssClassName);
            loadingImage.AddManipulator(new AssetScopeManipulator<Texture2D>
            {
                assetRef = "runios:ui/io_browser/loading",
                editorAssetRef = "runios-editor:ui/io_browser/loading",
                editorDarkAssetRef = "runios-editor:ui_dark/io_browser/loading",
                applyAsset = x => loadingImage.image = x,
                clearAsset = () => loadingImage.image = null
            });
            cell.Add(loadingImage);

            return cell;
        }

        static Func<VisualElement> MakeCell<T>(string name) where T : VisualElement, new() => () =>
        {
            T element = new T { name = name };
            element.AddToClassList(cellUssClassName);
            return element;
        };

        void BindNameCell(VisualElement element, int index)
        {
            IOEntry entry = treeView.GetItemDataForIndex<IOEntry>(index);
            int id = treeView.GetIdForIndex(index);

            AssetScopeManipulator<VectorImage> iconManipulator = (AssetScopeManipulator<VectorImage>)element.Q<Image>("icon").userData!;
            (iconManipulator.assetRef, iconManipulator.editorDarkAssetRef) = GetIcon(entry);
            element.Q<Label>("name-label").text = entry.metaData.name;
            element.EnableInClassList(nameCellLoadingUssClassName, loader.IsLoading(id));
            element.EnableInClassList(hiddenCellUssClassName, IsHidden(entry));
        }

        /// <summary>
        /// 항목에 표시할 아이콘과 에디터 다크 테마 아이콘을 가져옵니다.
        /// </summary>
        /// <remarks>
        /// 파일은 <c>a.tar.gz</c>라면 <c>tar.gz</c>, <c>gz</c> 순서로 확장자 아이콘을 찾습니다.
        /// </remarks>
        protected virtual (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) GetIcon(IOEntry entry)
        {
            if (entry.isDirectory)
                return (folderIcon, folderEditorDarkIcon);

            string name = entry.metaData.name.ToLowerInvariant();
            for (int i = name.IndexOf('.'); i >= 0; i = name.IndexOf('.', i + 1))
            {
                string extension = name[(i + 1)..];

                RuniPath path = extensionIconDirectory.path / extension;
                if (!Identifier.IsPathValid(path))
                    continue;

                Identifier icon = new Identifier(extensionIconDirectory.nameSpace, path);
                if (ResourceManager.GetHandle<VectorImage>(icon) != null)
                    return (icon, new Identifier(extensionEditorDarkIconDirectory.nameSpace, extensionEditorDarkIconDirectory.path / extension));
            }

            return (unknownIcon, unknownEditorDarkIcon);
        }

        void BindSizeCell(VisualElement element, int index)
        {
            IOEntry entry = treeView.GetItemDataForIndex<IOEntry>(index);
            int id = treeView.GetIdForIndex(index);

            element.EnableInClassList(hiddenCellUssClassName, IsHidden(entry));

            string text;
            if (entry.metaData.size is { } size)
                text = size.DataSizeToString();
            else if (loader.IsLoading(id) || loader.IsLoaded(id))
                text = string.Format(GetText("runios-editor:io_node_tree_view.item_count"), treeView.viewController.GetChildrenIds(id).Count());
            else
                text = string.Empty;

            ((Label)element).text = text;
        }

        int CompareDirectoryFirst(IOEntry x, IOEntry y) => y.isDirectory.CompareTo(x.isDirectory);

        // 컬럼 정렬에서도 폴더가 파일보다 먼저 오도록, 폴더 우선 비교 뒤에 컬럼 값을 비교합니다.
        Comparison<int> CompareByIndex(Column column, Comparison<IOEntry> comparison) => (a, b) =>
        {
            IOEntry x = treeView.GetItemDataForIndex<IOEntry>(a);
            IOEntry y = treeView.GetItemDataForIndex<IOEntry>(b);

            int result = CompareDirectoryFirst(x, y);
            if (result == 0)
                return comparison.Invoke(x, y);

            // Unity는 내림차순일 때 비교 결과의 부호를 뒤집으므로, 폴더 우선 결과를 미리 뒤집어 방향과 관계없이 폴더가 위에 오게 합니다.
            return GetSortDirection(column) == SortDirection.Descending ? -result : result;
        };

        SortDirection GetSortDirection(Column column)
        {
            foreach (SortColumnDescription description in treeView.sortedColumns)
            {
                if (description.column == column)
                    return description.direction;
            }

            return SortDirection.Ascending;
        }

        Action<VisualElement, int> BindCell<T>(Action<T, IOEntry> action) where T : VisualElement => (element, index) =>
        {
            T genericElement = (T)element;
            IOEntry entry = treeView.GetItemDataForIndex<IOEntry>(index);

            element.EnableInClassList(hiddenCellUssClassName, IsHidden(entry));
            action.Invoke(genericElement, entry);
        };

        void ShowContextMenu(Vector2 position)
        {
            IOEntry[] entries = selectedEntries;
            if (entries.Length == 0)
                return;

            GenericDropdownMenu menu = new();
            BuildContextMenu(menu, entries);
            menu.DropDown(new Rect(position, Vector2.zero), this, DropdownMenuSizeMode.Auto);
        }

        protected virtual void BuildContextMenu(GenericDropdownMenu menu, IOEntry[] entries)
        {
            string showInFileManagerText = GetText("runios-editor:io_node_tree_view.show_in_file_manager");

            // 실제 경로가 있는 항목이고 이 플랫폼에 파일 관리자 연동이 있을 때만 보여 줄 수 있습니다.
            if (IFileManagerProvider.instance is { } fileManager && provider is PhysicalIOProvider physicalProvider)
            {
                PhysicalPath[] paths = entries.Select(x => physicalProvider.targetPath.Combine(x.path)).ToArray();
                menu.AddItem(showInFileManagerText, false, () => fileManager.ShowItems(paths).Forget());
            }
            else
                menu.AddDisabledItem(showInFileManagerText, false);
        }

        public UniTask Refresh() => loader.Reset(EnumerateEntries(node.path));

        IUniTaskAsyncEnumerable<IOEntry> EnumerateEntries(RuniPath path)
        {
            IUniTaskAsyncEnumerable<IOEntry> entries = provider.EnumerateEntries(path, false);
            return showHiddenFiles ? entries : entries.Where(static x => !IsHidden(x));
        }

        static bool IsHidden(IOEntry entry) => entry.metaData.attributes?.HasFlag(FileAttributes.Hidden) == true;
    }
}
