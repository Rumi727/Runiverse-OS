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
        public const string nameCellRenamingUssClassName = "runios-io-node-tree-view__name-cell--renaming";
        public const string renameFieldUssClassName = "runios-io-node-tree-view__rename-field";

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

        // 행이 다시 바인딩되거나 재사용되어도 편집 상태가 유지되도록, 행 요소가 아니라 항목 id로 기록합니다.
        int? renamingId;

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

            // 편집 필드 안에서 누른 F2가 편집을 다시 시작해 입력한 내용을 덮어쓰지 않게 합니다.
            RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.F2 || renamingId != null)
                    return;

                BeginRename();
                e.StopPropagation();
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

        VisualElement MakeNameCell()
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

            TextField renameField = new() { name = "rename-field", isDelayed = true };
            renameField.AddToClassList(renameFieldUssClassName);
            renameField.RegisterValueChangedCallback(OnRenameFieldChanged);
            renameField.RegisterCallback<FocusOutEvent>(_ => EndRename());
            cell.Add(renameField);

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
            element.EnableInClassList(nameCellRenamingUssClassName, id == renamingId);
            element.EnableInClassList(hiddenCellUssClassName, IsHidden(entry));

            // 값이 확정될 때 어느 항목의 이름을 바꿀지 알 수 있도록 id를 둡니다.
            element.Q<TextField>("rename-field").userData = id;
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
            string renameText = GetText("runios-editor:io_node_tree_view.rename");
            if (entries.Length == 1 && provider is IWritableIOProvider)
                menu.AddItem(renameText, false, BeginRename);
            else
                menu.AddDisabledItem(renameText, false);

            menu.AddSeparator(string.Empty);

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

        /// <summary>
        /// 선택된 항목이 하나이고 쓰기 가능한 프로바이더일 때, 그 항목의 이름 셀을 편집 상태로 바꿉니다.
        /// </summary>
        public void BeginRename()
        {
            if (provider is not IWritableIOProvider || treeView.selectedIds.Count() != 1)
                return;

            BeginRename(treeView.selectedIds.First());
        }

        void BeginRename(int id)
        {
            renamingId = id;

            // 편집 필드가 보이도록 행을 화면에 두고 다시 바인딩합니다.
            treeView.ScrollToItemById(id);
            treeView.RefreshItems();

            TextField? renameField = treeView.GetRootElementForId(id)?.Q<TextField>("rename-field");
            if (renameField == null)
            {
                // 행이 아직 만들어지지 않았으면 포커스를 잃을 필드도 없으므로 편집 상태로 남기지 않습니다.
                EndRename();
                return;
            }

            IOEntry entry = treeView.GetItemDataForId<IOEntry>(id);
            string name = entry.metaData.name;

            // 파일은 확장자를 뺀 이름만 선택합니다. .bashrc처럼 점으로 시작하는 이름은 확장자가 없는 것으로 보고 전체를 선택합니다.
            int extensionIndex = name.LastIndexOf(FileExtension.extensionSeparatorChar);
            int selectionLength = !entry.isDirectory && extensionIndex > 0 ? extensionIndex : name.Length;

            renameField.SetValueWithoutNotify(name);

            // 편집 필드는 USS 클래스로 표시되므로, 스타일이 다시 계산되기 전에는 display: none이라 포커스를 받을 수 없습니다.
            // 스케줄러는 스타일·레이아웃 갱신과 따로 돌아서 다음 틱에도 아직 숨겨져 있을 수 있으므로, 필드가 배치된 뒤에 포커스합니다.
            renameField.RegisterCallback<GeometryChangedEvent>(OnRenameFieldGeometryChanged);

            void OnRenameFieldGeometryChanged(GeometryChangedEvent _)
            {
                renameField.UnregisterCallback<GeometryChangedEvent>(OnRenameFieldGeometryChanged);

                // 이벤트 처리 중에 포커스하면 포커스 이벤트가 이 처리 뒤로 미뤄져 기본 전체 선택이 선택 범위를 덮으므로, 다음 틱에 포커스합니다.
                renameField.schedule.Execute(() =>
                {
                    if (renamingId != id)
                        return;

                    renameField.Focus();
                    renameField.SelectRange(selectionLength, 0);
                });
            }
        }

        void EndRename()
        {
            renamingId = null;
            treeView.RefreshItems();
        }

        void OnRenameFieldChanged(ChangeEvent<string> evt)
        {
            if (evt.target is not TextField { userData: int id })
                return;

            Rename(id, evt.newValue).Forget();
        }

        async UniTask Rename(int id, string name)
        {
            IOEntry entry = treeView.GetItemDataForId<IOEntry>(id);
            if (name == entry.metaData.name || node.AsWriteNode() is not { } writeNode)
                return;

            // RuniPath에서 /는 구분자라, 이름에 들어가면 하위 경로로 이동해 버리므로 이름으로 받지 않습니다.
            if (name.Contains(RuniPath.directorySeparatorChar))
            {
                Debug.LogError($"The name '{name}' cannot contain '{RuniPath.directorySeparatorChar}'.");
                return;
            }

            RuniPath destinationPath = entry.path.GetParentPath() / name;
            IOWriteNode entryNode = writeNode.Bind(entry);

            // 대상이 이미 있는지는 덮어쓰지 않는 이동이 판단하므로 미리 확인하지 않습니다.
            try
            {
                if (entry.isDirectory)
                    await entryNode.dir.Move(destinationPath);
                else
                    await entryNode.file.Move(destinationPath);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            // 이동하는 동안 트리가 다시 로드되었으면 이미 바뀐 이름으로 표시되어 있습니다.
            if (!treeView.viewController.Exists(id))
                return;

            // 이름을 바꾼 디렉터리의 자식들은 이전 경로를 가지고 있으므로, 항목을 새로 추가해 자식을 다시 로드하게 합니다.
            int parentId = treeView.viewController.GetParentId(id);
            bool isExpanded = treeView.IsExpanded(id);

            // Enter로 확정하면 아직 편집 필드에 포커스가 있으므로, 제거되는 항목의 편집 상태를 지우고 숨겨질 필드 대신 트리에 포커스를 둡니다.
            if (renamingId == id)
            {
                renamingId = null;
                treeView.Focus();
            }

            loader.RemoveItem(id);
            int renamedId = loader.AddItem(entry with { path = destinationPath, metaData = entry.metaData with { name = name } }, parentId);

            treeView.RefreshItems();
            if (isExpanded)
                treeView.ExpandItem(renamedId);

            treeView.SetSelectionById(renamedId);
            treeView.ScrollToItemById(renamedId);
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
