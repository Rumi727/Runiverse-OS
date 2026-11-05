#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO;
using RuniOS.IO.Locations;
using RuniOS.Resource;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class FileSystemBrowser : IOBrowser
    {
        public new const string ussClassName = "runios-file-system-browser";
        public const string placeSpaceBarUssClassName = "runios-file-system-browser__place-space-bar";

        static readonly KnownDirectoryType[] placeDirectoryTypes =
        [
            KnownDirectoryType.home,
            KnownDirectoryType.desktop,
            KnownDirectoryType.documents,
            KnownDirectoryType.downloads,
            KnownDirectoryType.music,
            KnownDirectoryType.pictures,
            KnownDirectoryType.videos
        ];

        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) folderIcon = ("runios-ui:ui/io_browser/places/16/folder", "runios-ui:ui_dark/io_browser/places/16/folder");
        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) computerIcon = ("runios-ui:ui/io_browser/devices/16/computer", "runios-ui:ui_dark/io_browser/devices/16/computer");
        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) networkIcon = ("runios-ui:ui/io_browser/places/16/network-workgroup", "runios-ui:ui_dark/io_browser/places/16/network-workgroup");
        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) opticalIcon = ("runios-ui:ui/io_browser/devices/16/media-optical", "runios-ui:ui_dark/io_browser/devices/16/media-optical");
        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) removableIcon = ("runios-ui:ui/io_browser/devices/16/drive-removable-media", "runios-ui:ui_dark/io_browser/devices/16/drive-removable-media");
        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) harddiskIcon = ("runios-ui:ui/io_browser/devices/16/drive-harddisk", "runios-ui:ui_dark/io_browser/devices/16/drive-harddisk");

        static readonly Dictionary<KnownDirectoryType, (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon)> knownDirectoryIcons = new()
        {
            [KnownDirectoryType.home] = ("runios-ui:ui/io_browser/places/16/user-home", "runios-ui:ui_dark/io_browser/places/16/user-home"),
            [KnownDirectoryType.desktop] = ("runios-ui:ui/io_browser/places/16/user-desktop", "runios-ui:ui_dark/io_browser/places/16/user-desktop"),
            [KnownDirectoryType.documents] = ("runios-ui:ui/io_browser/places/16/folder-documents", "runios-ui:ui_dark/io_browser/places/16/folder-documents"),
            [KnownDirectoryType.downloads] = ("runios-ui:ui/io_browser/places/16/folder-download", "runios-ui:ui_dark/io_browser/places/16/folder-download"),
            [KnownDirectoryType.music] = ("runios-ui:ui/io_browser/places/16/folder-music", "runios-ui:ui_dark/io_browser/places/16/folder-music"),
            [KnownDirectoryType.pictures] = ("runios-ui:ui/io_browser/places/16/folder-pictures", "runios-ui:ui_dark/io_browser/places/16/folder-pictures"),
            [KnownDirectoryType.videos] = ("runios-ui:ui/io_browser/places/16/folder-videos", "runios-ui:ui_dark/io_browser/places/16/folder-videos"),
            [KnownDirectoryType.templates] = ("runios-ui:ui/io_browser/places/16/folder-templates", "runios-ui:ui_dark/io_browser/places/16/folder-templates"),
            [KnownDirectoryType.publicShare] = ("runios-ui:ui/io_browser/places/16/folder-publicshare", "runios-ui:ui_dark/io_browser/places/16/folder-publicshare"),
            [KnownDirectoryType.favorites] = ("runios-ui:ui/io_browser/places/16/folder-favorites", "runios-ui:ui_dark/io_browser/places/16/folder-favorites"),
            [KnownDirectoryType.savedGames] = ("runios-ui:ui/io_browser/places/16/folder-games", "runios-ui:ui_dark/io_browser/places/16/folder-games")
        };

        // 같은 경로가 같은 노드가 되도록 경로 루트(리눅스는 /, 윈도우는 C:\, D:\ 등)마다 프로바이더를 하나만 둡니다.
        static readonly Dictionary<PhysicalPath, PhysicalIOProvider> rootProviders = new();

        /// <summary>
        /// 활성 탭의 장치 목록입니다.
        /// </summary>
        public DeviceLocationView deviceLocationView => ((FileSystemBrowserTab)activeTab).deviceLocationView;

        /// <summary>
        /// 활성 탭의 트리 뷰입니다.
        /// </summary>
        public IONodeMultiColumnTreeView treeView => ((FileSystemBrowserTab)activeTab).treeView;

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
                    ((FileSystemBrowserTab)tab).selectionType = value;
            }
        } = SelectionType.Multiple;

        public FileSystemBrowser()
        {
            this.AddManipulator(new LocalizationManipulator(() => RefreshNavigation().Forget()));
            AddToClassList(ussClassName);

            AddTab(IONode.empty);
        }

        protected override IOBrowserTab CreateTab() => new FileSystemBrowserTab { selectionType = selectionType };

        protected override void ToggleShowHiddenFiles() => treeView.showHiddenFiles = !treeView.showHiddenFiles;

        protected override void BuildFolderMenu(RuniMenu menu)
        {
            // 홈 화면(장치 목록)에서는 트리 뷰가 이전 폴더를 가리킨 채 숨겨져 있으므로 만들 수 없게 합니다.
            IONodeMultiColumnTreeView.AddNewItemMenu(menu, () => node == IONode.empty ? null : treeView);
            menu.Add(new Separator());

            base.BuildFolderMenu(menu);

            RuniMenuItem showHiddenFiles = new(string.Empty, ToggleShowHiddenFiles) { shortcuts = [showHiddenFilesShortcut] };
            menu.opened += () =>
            {
                showHiddenFiles.text = GetText("runios-editor:io_browser.show_hidden_files");
                showHiddenFiles.isChecked = treeView.showHiddenFiles;
            };

            menu.Add(showHiddenFiles);
        }

        // 루트에서 위로 이동하면 홈 화면으로 돌아갑니다.
        public override void Up()
        {
            if (node.path.IsEmpty())
                node = IONode.empty;
            else
                base.Up();
        }

        public async UniTask RefreshNavigation()
        {
            List<VisualElement> groups = [];

            IOBrowserPlaceGroup places = new(GetText("runios-editor:io_browser.places")) { name = "places" };
            places.Add(MakePlace(GetText("runios-editor:io_browser.computer"), computerIcon, () => node = IONode.empty));
            places.Add(MakeCurrentDirectoryPlace());

            if (IKnownDirectoryProvider.instance is { } knownDirectoryProvider)
            {
                List<KnownDirectory> directories = await knownDirectoryProvider.EnumerateDirectories().ToListAsync();
                foreach (KnownDirectoryType type in placeDirectoryTypes)
                {
                    foreach (KnownDirectory directory in directories)
                    {
                        if (directory.type != type)
                            continue;

                        places.Add(MakeKnownDirectoryPlace(directory));
                        break;
                    }
                }

                foreach (KnownDirectory directory in directories)
                {
                    if (!placeDirectoryTypes.Contains(directory.type))
                        places.Add(MakeKnownDirectoryPlace(directory));
                }
            }

            groups.Add(places);

            if (INetworkLocationProvider.instance is { } networkLocationProvider)
            {
                IOBrowserPlaceGroup remote = new(GetText("runios-editor:io_browser.remote")) { name = "remote" };
                await foreach (NetworkLocation location in networkLocationProvider.EnumerateLocations())
                    remote.Add(MakeLocationPlace(location, networkIcon));

                if (remote.childCount > 0)
                    groups.Add(remote);
            }

            if (IDeviceLocationProvider.instance is { } deviceLocationProvider)
            {
                IOBrowserPlaceGroup devices = new(GetText("runios-editor:io_browser.devices")) { name = "devices" };
                await foreach (DeviceLocation location in deviceLocationProvider.EnumerateLocations())
                    devices.Add(MakeDevicePlace(location));

                if (devices.childCount > 0)
                    groups.Add(devices);
            }

            // 열거가 끝난 뒤 한 번에 교체하므로, 겹쳐 호출되어도 항목이 중복되지 않습니다.
            navigationPane.Clear();
            foreach (VisualElement group in groups)
                navigationPane.Add(group);
        }

        // 프로세스의 작업 폴더이며, 에디터에서는 프로젝트 폴더, 빌드에서는 실행한 위치입니다.
        IOBrowserPlace MakeCurrentDirectoryPlace()
        {
            PhysicalPath path = PhysicalPath.From(Environment.CurrentDirectory);

            IOBrowserPlace place = MakePlace(GetText("runios-editor:io_browser.current_directory"), folderIcon, () => Open(path));
            place.name = "current-directory";
            place.dropDirectory = GetNodeFromPhysicalRoot(path);

            return place;
        }

        // 번역 키가 없는 종류는 종류 이름을 그대로 표시합니다.
        IOBrowserPlace MakeKnownDirectoryPlace(KnownDirectory directory)
        {
            string text = LocalizationUtility.GetText($"runios-editor:io_browser.known_directory.{directory.type}") ?? directory.type;
            IOBrowserPlace place = MakePlace(text, knownDirectoryIcons.GetValueOrDefault(directory.type, folderIcon), () => Open(directory.path));
            place.name = directory.type;
            place.dropDirectory = GetNodeFromPhysicalRoot(directory.path);

            return place;
        }

        static IOBrowserPlace MakePlace(string text, (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) icons, Action clicked)
        {
            IOBrowserPlace place = new(text, clicked)
            {
                iconManipulator =
                {
                    assetRef = icons.icon,
                    editorDarkAssetRef = icons.editorDarkIcon
                }
            };

            return place;
        }

        // 읽기 전용 위치에는 끌어다 놓을 수 없습니다.
        IOBrowserPlace MakeLocationPlace(FileLocation location, (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) icons)
        {
            IOBrowserPlace place = MakePlace(location.displayName, icons, () => OpenLocation(location));
            if (!location.isReadOnly)
                place.dropDirectory = GetNodeFromPhysicalRoot(location.ioProvider.rootNode);

            return place;
        }

        IOBrowserPlace MakeDevicePlace(DeviceLocation location)
        {
            (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) icons = location.isOptical ? opticalIcon : location.isRemovable ? removableIcon : harddiskIcon;
            IOBrowserPlace place = MakeLocationPlace(location, icons);

            if (location.TryGetSpaceInfo(out StorageSpaceInfo info) && info.totalSize > 0)
            {
                ProgressBar spaceBar = new()
                {
                    name = "space-bar",
                    lowValue = 0,
                    highValue = info.totalSize,
                    value = info.totalSize - info.availableSize
                };
                spaceBar.AddToClassList(placeSpaceBarUssClassName);
                place.Add(spaceBar);
            }

            return place;
        }

        public void Open(PhysicalPath path) => node = GetNodeFromPhysicalRoot(path);

        protected override string NodeToPathField(IONode node)
        {
            if (node.provider is PhysicalIOProvider physicalProvider)
                return physicalProvider.targetPath.Combine(node.path);

            return node.path.value;
        }

        // 홈 화면(장치 목록) 버튼 뒤에, 경로 루트(리눅스는 /, 윈도우는 C:\ 등)부터 OS 경로 그대로 이어 붙입니다.
        protected override IEnumerable<(string name, IONode node)> NodeToBreadcrumbs(IONode node)
        {
            yield return (GetText("runios-editor:io_browser.computer"), IONode.empty);

            if (node.provider is not PhysicalIOProvider physicalProvider)
                yield break;

            yield return (physicalProvider.targetPath.ToString(), node.rootNode);

            foreach ((string name, IONode node) crumb in base.NodeToBreadcrumbs(node).Skip(1))
                yield return crumb;
        }

        // 빈 경로는 홈 화면을 뜻합니다.
        // 바로 가기 등으로 다른 위치를 가리켜도 접근할 수 있도록, 샌드박스 없이 경로 루트에서부터 엽니다.
        protected override IONode PathFieldToNode(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return IONode.empty;

            return GetNodeFromPhysicalRoot(PhysicalPath.From(path));
        }

        public void OpenLocation(FileLocation location) => node = GetNodeFromPhysicalRoot(location.ioProvider.rootNode);

        static string GetText(string key) => LocalizationUtility.GetText(key) ?? key;

        /// <summary>
        /// 물리 루트에서 시작된 노드를 반환합니다.
        /// </summary>
        public static IONode GetNodeFromPhysicalRoot(PhysicalPath path)
        {
            PhysicalPath targetPath = path;
            PhysicalPath root = PhysicalPath.From(targetPath.GetPathRoot());

            if (!rootProviders.TryGetValue(root, out PhysicalIOProvider? provider))
                rootProviders[root] = provider = new PhysicalIOProvider(root, SandboxPolicy.Unrestricted);

            return provider.rootNode.CreateChild(targetPath.GetRelativePath(root));
        }

        /// <summary>
        /// 물리 경로 노드일 경우, 루트에서 시작된 노드를 반환합니다.
        /// </summary>
        public static IONode GetNodeFromPhysicalRoot(IONode node)
        {
            if (node.provider is not PhysicalIOProvider physicalProvider)
                return node;

            return GetNodeFromPhysicalRoot(physicalProvider.targetPath.Combine(node.path));
        }

        /// <summary>
        /// 물리 경로 위치일 경우, 루트에서 시작된 노드를 반환합니다.
        /// </summary>
        public static IONode GetNodeFromPhysicalRoot(FileLocation location) => GetNodeFromPhysicalRoot(location.ioProvider.rootNode);
    }
}
