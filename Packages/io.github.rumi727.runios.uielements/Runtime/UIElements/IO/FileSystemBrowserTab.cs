#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class FileSystemBrowserTab : IOBrowserTab
    {
        public override IONode node
        {
            get => base.node;
            set => base.node = FileSystemBrowser.GetNodeFromPhysicalRoot(value);
        }

        [UxmlAttribute]
        public SelectionType selectionType
        {
            get;
            set
            {
                field = value;
                treeView.selectionType = value;
            }
        } = SelectionType.Multiple;

        public DeviceLocationView deviceLocationView { get; }
        public IONodeMultiColumnTreeView treeView { get; }

        public FileSystemBrowserTab()
        {
            deviceLocationView = new DeviceLocationView { name = "device-location-view" };
            deviceLocationView.opened += x => node = FileSystemBrowser.GetNodeFromPhysicalRoot(x);
            Add(deviceLocationView);

            treeView = new IONodeMultiColumnTreeView { name = "tree-view" };
            treeView.opened += OpenEntry;
            Add(treeView);

            this.AddManipulator(new LocalizationManipulator(RefreshLabel));
        }

        // 노드가 없으면 홈 화면으로 장치 목록을 표시합니다.
        protected override void OnNodeChanged()
        {
            bool isHome = node == IONode.empty;

            deviceLocationView.style.display = isHome ? DisplayStyle.Flex : DisplayStyle.None;
            treeView.style.display = isHome ? DisplayStyle.None : DisplayStyle.Flex;

            Refresh().Forget();
        }

        // 홈 화면은 장치 목록의 이름으로 표시합니다.
        protected override string GetLabel()
        {
            if (node == IONode.empty)
                return LocalizationUtility.GetTextOrKey("runios-editor:io_browser.computer");

            if (string.IsNullOrEmpty(node.name) && node.provider is PhysicalIOProvider provider)
                return provider.targetPath;

            return base.GetLabel();
        }

        public UniTask Refresh()
        {
            if (node == IONode.empty)
                return deviceLocationView.Refresh();

            treeView.node = node;
            return treeView.Refresh();
        }
    }
}
