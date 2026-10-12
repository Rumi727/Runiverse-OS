#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IONodeMultiColumnTreeView : VisualElement
    {
        public const string ussClassName = "runios-io-node-tree-view";
        public const string cellUssClassName = "runios-io-node-tree-view__cell";
        public const string nameCellUssClassName = "runios-io-node-tree-view__name-cell";
        public const string nameCellLoadingUssClassName = "runios-io-node-tree-view__name-cell--loading";
        public const string nameLabelUssClassName = "runios-io-node-tree-view__name-label";
        public const string loadingImageUssClassName = "runios-io-node-tree-view__loading-image";

        public IIOProvider provider => node.provider;
        public IONode node { get; set; }

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

            treeView = new MultiColumnTreeView([nameColumn, sizeColumn, creationTimeColumn, lastAccessTime, lastWriteTimeColumn]) { sortingMode = ColumnSortingMode.Default };
            treeView.sortColumnDescriptions.Add(new SortColumnDescription(nameColumn.name, SortDirection.Ascending));

            loader = new LazyTreeLoader<IOEntry>(treeView, x => x.isDirectory, x => provider.EnumerateEntries(x.path, false)) { preloadDepth = 1 };
            treeView.SetViewController(new LazyMultiColumnTreeViewController<IOEntry>(treeView, loader));

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

            Label label = new() { name = "name-label" };
            label.AddToClassList(nameLabelUssClassName);
            cell.Add(label);

            Image loadingImage = new() { name = "loading-image" };
            loadingImage.AddToClassList(loadingImageUssClassName);
            loadingImage.AddManipulator(new AssetScopeManipulator<Texture2D>
            {
                assetRef = "runios:ui/io_node_browser/loading",
                editorAssetRef = "runios-editor:ui/io_node_browser/loading",
                editorDarkAssetRef = "runios-editor:ui_dark/io_node_browser/loading",
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

            element.Q<Label>("name-label").text = entry.metaData.name;
            element.EnableInClassList(nameCellLoadingUssClassName, loader.IsLoading(id));
        }

        void BindSizeCell(VisualElement element, int index)
        {
            IOEntry entry = treeView.GetItemDataForIndex<IOEntry>(index);
            int id = treeView.GetIdForIndex(index);

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
            action.Invoke(genericElement, entry);
        };

        public UniTask Refresh() => loader.Reset(provider.EnumerateEntries(node.path, false));
    }
}
