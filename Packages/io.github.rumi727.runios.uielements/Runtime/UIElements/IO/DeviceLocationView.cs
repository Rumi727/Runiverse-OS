#nullable enable
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using RuniOS.IO.Locations;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class DeviceLocationView : VisualElement
    {
        public const string ussClassName = "runios-device-location-view";
        public const string itemUssClassName = "runios-device-location-view__item";
        public const string itemLabelUssClassName = "runios-device-location-view__item-label";
        public const string itemSpaceBarUssClassName = "runios-device-location-view__item-space-bar";

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

            Label label = new(location.displayName) { name = "label" };
            label.AddToClassList(itemLabelUssClassName);
            item.Add(label);

            if (location.TryGetSpaceInfo(out StorageSpaceInfo info) && info.totalSize > 0)
            {
                ProgressBar spaceBar = new()
                {
                    name = "space-bar",
                    lowValue = 0,
                    highValue = info.totalSize,
                    value = info.totalSize - info.availableSize,
                    title = $"{((long)info.availableSize).DataSizeToString()} / {((long)info.totalSize).DataSizeToString()}"
                };
                spaceBar.AddToClassList(itemSpaceBarUssClassName);
                item.Add(spaceBar);
            }

            return item;
        }
    }
}
