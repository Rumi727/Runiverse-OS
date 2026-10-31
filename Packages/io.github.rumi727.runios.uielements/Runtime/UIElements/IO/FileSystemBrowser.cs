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

        public DeviceLocationView deviceLocationView { get; }
        public IONodeMultiColumnTreeView treeView { get; }

        [UxmlAttribute]
        public SelectionType selectionType
        {
            get => treeView.selectionType;
            set => treeView.selectionType = value;
        }

        public FileSystemBrowser()
        {
            deviceLocationView = new DeviceLocationView { name = "device-location-view" };
            deviceLocationView.opened += OpenLocation;
            contentPane.Add(deviceLocationView);

            treeView = new IONodeMultiColumnTreeView { name = "tree-view" };
            treeView.opened += OpenEntry;
            contentPane.Add(treeView);

            this.AddManipulator(new LocalizationManipulator(() => RefreshNavigation().Forget()));

            OnNodeChanged();
        }

        protected override void BuildFolderMenu(GenericDropdownMenu menu)
        {
            base.BuildFolderMenu(menu);
            menu.AddItem(GetText("runios-editor:io_browser.show_hidden_files"), treeView.showHiddenFiles, () => treeView.showHiddenFiles = !treeView.showHiddenFiles);
        }

        // 노드가 없으면 홈 화면으로 장치 목록을 표시합니다.
        protected override void OnNodeChanged()
        {
            bool isHome = node == IONode.empty;

            deviceLocationView.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            treeView.style.display = isHome ? DisplayStyle.None : DisplayStyle.Flex;

            Refresh().Forget();
        }

        // 루트에서 위로 이동하면 홈 화면으로 돌아갑니다.
        public override void Up()
        {
            if (node.path.IsEmpty())
                node = IONode.empty;
            else
                base.Up();
        }

        public UniTask Refresh()
        {
            if (node == IONode.empty)
                return deviceLocationView.Refresh();

            treeView.node = node;
            return treeView.Refresh();
        }

        public async UniTask RefreshNavigation()
        {
            List<VisualElement> groups = [];

            IOBrowserPlaceGroup places = new(GetText("runios-editor:io_browser.places")) { name = "places" };
            places.Add(MakePlace(GetText("runios-editor:io_browser.computer"), computerIcon, () => node = IONode.empty));

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
                    remote.Add(MakePlace(location.displayName, networkIcon, () => OpenLocation(location)));

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

        // 번역 키가 없는 종류는 종류 이름을 그대로 표시합니다.
        IOBrowserPlace MakeKnownDirectoryPlace(KnownDirectory directory)
        {
            string text = LocalizationUtility.GetText($"runios-editor:io_browser.known_directory.{directory.type}") ?? directory.type;
            IOBrowserPlace place = MakePlace(text, knownDirectoryIcons.GetValueOrDefault(directory.type, folderIcon), () => Open(directory.path));
            place.name = directory.type;

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

        IOBrowserPlace MakeDevicePlace(DeviceLocation location)
        {
            (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) icons = location.isOptical ? opticalIcon : location.isRemovable ? removableIcon : harddiskIcon;
            IOBrowserPlace place = MakePlace(location.displayName, icons, () => OpenLocation(location));

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

        public void Open(PhysicalPath path) => node = ToNode(path);

        protected override string ToPath(IONode node)
        {
            if (node.provider is PhysicalIOProvider physicalProvider)
                return physicalProvider.targetPath.Combine(node.path);

            return string.Empty;
        }

        // 빈 경로는 홈 화면을 뜻합니다.
        // 바로 가기 등으로 다른 위치를 가리켜도 접근할 수 있도록, 샌드박스 없이 경로 루트에서부터 엽니다.
        protected override IONode ToNode(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return IONode.empty;

            PhysicalPath physicalPath = PhysicalPath.From(path);
            PhysicalPath root = PhysicalPath.From(physicalPath.GetPathRoot());

            if (!rootProviders.TryGetValue(root, out PhysicalIOProvider? provider))
                rootProviders[root] = provider = new PhysicalIOProvider(root, SandboxPolicy.Disabled);

            return provider.rootNode.CreateChild(physicalPath.GetRelativePath(root));
        }

        public void OpenLocation(FileLocation location)
        {
            if (location.ioProvider is PhysicalIOProvider physicalProvider)
                Open(physicalProvider.targetPath);
            else
                node = location.ioProvider.rootNode;
        }

        static string GetText(string key) => LocalizationUtility.GetText(key) ?? key;
    }
}
