#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO.Locations;
using RuniOS.Resource;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class DeviceLocationView : VisualElement
    {
        public const string ussClassName = "runios-device-location-view";
        public const string itemUssClassName = "runios-device-location-view__item";
        public const string itemIconUssClassName = "runios-device-location-view__item-icon";
        public const string itemInfoUssClassName = "runios-device-location-view__item-info";
        public const string itemLabelUssClassName = "runios-device-location-view__item-label";
        public const string itemSpaceBarUssClassName = "runios-device-location-view__item-space-bar";

        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) opticalIcon = ("runios-ui:ui/io_browser/devices/64/media-optical", "runios-ui:ui_dark/io_browser/devices/64/media-optical");
        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) removableIcon = ("runios-ui:ui/io_browser/devices/64/drive-removable-media", "runios-ui:ui_dark/io_browser/devices/64/drive-removable-media");
        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) harddiskIcon = ("runios-ui:ui/io_browser/devices/64/drive-harddisk", "runios-ui:ui_dark/io_browser/devices/64/drive-harddisk");

        public event Action<DeviceLocation>? selected;

        public DeviceLocationView()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));

            AddToClassList(ussClassName);
        }

        public async UniTask Refresh()
        {
            if (IDeviceLocationProvider.instance is not { } provider)
            {
                Clear();
                return;
            }

            // 열거가 끝난 뒤 한 번에 교체하므로, 겹쳐 호출되어도 항목이 중복되지 않습니다.
            List<DeviceLocation> locations = await provider.EnumerateLocations().ToListAsync();

            Clear();
            foreach (DeviceLocation location in locations)
                Add(MakeItem(location));
        }

        VisualElement MakeItem(DeviceLocation location)
        {
            Button item = new(() => selected?.Invoke(location)) { name = "item" };
            item.AddToClassList(itemUssClassName);

            (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) icons = location.isOptical ? opticalIcon : location.isRemovable ? removableIcon : harddiskIcon;
            Image icon = new() { name = "icon" };
            icon.AddToClassList(itemIconUssClassName);
            icon.AddManipulator(new AssetScopeManipulator<VectorImage>
            {
                assetRef = icons.icon,
                editorDarkAssetRef = icons.editorDarkIcon,
                applyAsset = x => icon.vectorImage = x,
                clearAsset = () => icon.vectorImage = null
            });
            item.Add(icon);

            VisualElement info = new() { name = "info" };
            info.AddToClassList(itemInfoUssClassName);
            item.Add(info);

            Label label = new(location.displayName) { name = "label" };
            label.AddToClassList(itemLabelUssClassName);
            info.Add(label);

            if (location.TryGetSpaceInfo(out StorageSpaceInfo spaceInfo) && spaceInfo.totalSize > 0)
            {
                ProgressBar spaceBar = new()
                {
                    name = "space-bar",
                    lowValue = 0,
                    highValue = spaceInfo.totalSize,
                    value = spaceInfo.totalSize - spaceInfo.availableSize,
                    title = $"{((long)spaceInfo.availableSize).DataSizeToString()} / {((long)spaceInfo.totalSize).DataSizeToString()}"
                };
                spaceBar.AddToClassList(itemSpaceBarUssClassName);
                info.Add(spaceBar);
            }

            return item;
        }
    }
}
