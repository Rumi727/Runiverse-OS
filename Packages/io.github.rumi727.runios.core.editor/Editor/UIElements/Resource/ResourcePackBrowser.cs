#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.Editor.Resource;
using RuniOS.IO;
using RuniOS.Resource;
using RuniOS.UIElements;
using RuniOS.UIElements.IO;
using System.Collections.Immutable;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Resource
{
    /// <summary>
    /// Browses every streaming assets location that the Editor can see, and reports the selected nodes regardless of tabs.<br/>
    /// 에디터에서 볼 수 있는 모든 StreamingAssets 위치를 탐색하고, 탭과 관계없이 선택한 노드를 알립니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The navigation pane groups the locations of <see cref="EditorStreamingAssetsService.sources"/> and <see cref="EditorStreamingAssetsService.editorSources"/> that exist, and <c>.meta</c> files are not shown.<br/>
    /// <see cref="selectedNodes"/> follows the active tab. When nothing is selected, it contains the current folder of the active tab, so moving to another folder or switching tabs also changes it.
    /// </para>
    /// <br/><br/>
    /// <para>
    /// 탐색 영역은 <see cref="EditorStreamingAssetsService.sources"/>와 <see cref="EditorStreamingAssetsService.editorSources"/> 중 실제로 있는 위치를 묶어 보여주고, <c>.meta</c> 파일은 보여주지 않습니다.<br/>
    /// <see cref="selectedNodes"/>는 활성 탭을 따릅니다. 아무것도 선택하지 않았으면 활성 탭의 현재 폴더가 들어 있으므로, 다른 폴더로 이동하거나 탭을 바꿔도 바뀝니다.
    /// </para>
    /// </remarks>
    public class ResourcePackBrowser : IONodeBrowser
    {
        public new const string ussClassName = "runios-resource-pack-browser";

        static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) folderIcon = ("runios-ui:ui/io_browser/places/16/folder", "runios-ui:ui_dark/io_browser/places/16/folder");

        /// <summary>
        /// Gets the selected nodes of the active tab, or its current folder when nothing is selected.<br/>
        /// 활성 탭에서 선택한 노드를 가져오며, 아무것도 선택하지 않았으면 그 탭의 현재 폴더를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// Nodes outside the streaming assets locations, such as <see cref="IONode.empty"/> before a location is opened, are not included.<br/>
        /// 위치를 열기 전의 <see cref="IONode.empty"/>처럼 StreamingAssets 위치 밖의 노드는 들어 있지 않습니다.
        /// </remarks>
        public ImmutableArray<IONode> selectedNodes { get; private set; } = [];

        /// <summary>
        /// Occurs before <see cref="selectedNodes"/> changes, with the new nodes. If any subscriber returns <see langword="false"/>, the change is canceled.<br/>
        /// This also occurs when moving to another folder, switching tabs, or closing the active tab changes <see cref="selectedNodes"/>.
        /// <br/><br/>
        /// <see cref="selectedNodes"/>가 바뀌기 전에 새 노드와 함께 발생합니다. 구독자가 하나라도 <see langword="false"/>를 반환하면 바뀌지 않습니다.<br/>
        /// 다른 폴더로 이동하거나, 탭을 바꾸거나, 활성 탭을 닫아 <see cref="selectedNodes"/>가 바뀔 때도 발생합니다.
        /// </summary>
        public event Func<ImmutableArray<IONode>, bool>? selectedNodesChanging;

        /// <summary>
        /// Occurs after <see cref="selectedNodes"/> changes, with the new nodes.<br/>
        /// <see cref="selectedNodes"/>가 바뀐 뒤에 새 노드와 함께 발생합니다.
        /// </summary>
        /// <remarks>
        /// Changes that cannot be canceled, such as a refresh or items moved or deleted in this browser, raise only this event.<br/>
        /// 새로 고침이나 이 브라우저에서 옮기거나 지운 항목처럼 취소할 수 없는 변경은 이 이벤트만 발생시킵니다.
        /// </remarks>
        public event Action<ImmutableArray<IONode>>? selectedNodesChanged;

        public ResourcePackBrowser()
        {
            AddToClassList(ussClassName);

            filter = new NotPatternMatcher(new ExtensionMatcher(".meta"));

            selectionChanging += (tab, entries) => tab != activeTab || CanChange(GetNodes(tab.node, entries));
            selectionChanged += (tab, entries) =>
            {
                if (tab == activeTab)
                    Accept(tab.node, GetNodes(tab.node, entries));
            };

            // 다른 폴더로 이동하면 선택이 모두 해제되므로, 새 폴더를 선택한 것으로 봅니다.
            // 같은 위치를 다시 열면 새로 고침이 트리를 잠시 비우지만 선택은 되살아나므로, 위치가 바뀐 때만 다시 계산합니다.
            nodeChanging += (tab, node) => tab != activeTab || CanChange(GetNodes(node, []));
            nodeChanged += tab =>
            {
                if (tab == activeTab && tab.node != selectedNodesDirectory)
                    Accept(tab.node, GetSelectedNodes(tab));
            };

            // 활성 탭을 닫으면 옆 탭을 먼저 활성화하므로, 닫기도 여기서 묻습니다.
            activeTabChanging += (_, tab) => CanChange(GetSelectedNodes(tab));
            activeTabChanged += (_, tab) => Accept(tab.node, GetSelectedNodes(tab));

            moved += (_, moves) => Accept(selectedNodesDirectory, [..selectedNodes.Select(x => GetMovedNode(x, moves))]);
            deleted += (_, nodes) => Accept(selectedNodesDirectory, [..selectedNodes.Where(x => !nodes.Any(y => IsSameOrDescendant(x, y)))]);

            this.AddManipulator(new LocalizationManipulator(() => RefreshNavigation().Forget()));

            // 정적 이벤트라 패널에서 떼어질 때 구독을 해제해야 이 요소가 남지 않습니다.
            RegisterCallback<AttachToPanelEvent>(_ => EditorApplication.projectChanged += OnProjectChanged);
            RegisterCallback<DetachFromPanelEvent>(_ => EditorApplication.projectChanged -= OnProjectChanged);
        }

        /// <summary>
        /// Selects the specified nodes in the active tab and sets <see cref="selectedNodes"/> without raising <see cref="selectedNodesChanging"/> or <see cref="selectedNodesChanged"/>.<br/>
        /// Nodes that are not loaded yet are selected as the tree loads them. Passing the current folder of the active tab, or nothing, clears the selection of the tab.
        /// <br/><br/>
        /// 활성 탭에서 지정한 노드를 선택하고, <see cref="selectedNodesChanging"/>과 <see cref="selectedNodesChanged"/>를 발생시키지 않고 <see cref="selectedNodes"/>를 정합니다.<br/>
        /// 아직 불러오지 않은 노드는 트리가 불러오는 대로 선택됩니다. 활성 탭의 현재 폴더를 넘기거나 아무것도 넘기지 않으면 그 탭의 선택을 해제합니다.
        /// </summary>
        /// <param name="nodes">
        /// The nodes to select. Nodes outside the current location of the active tab are ignored.<br/>
        /// 선택할 노드입니다. 활성 탭의 현재 위치 밖에 있는 노드는 무시합니다.
        /// </param>
        public void SetSelectedNodesWithoutNotify(IEnumerable<IONode> nodes)
        {
            IONode tabNode = activeTab.node;
            ImmutableArray<IONode> inLocation = [..nodes.Where(x => x.provider == tabNode.provider)];

            treeView.SetSelectionWithoutNotify(inLocation.Where(x => x != tabNode).Select(static x => x.path));

            selectedNodesDirectory = tabNode;
            selectedNodes = inLocation.Length > 0 ? inLocation : GetNodes(tabNode, []);
        }

        /// <summary>
        /// Selects the specified nodes in the active tab. This raises <see cref="selectedNodesChanging"/> and <see cref="selectedNodesChanged"/> as if the user selected them.<br/>
        /// Passing the current folder of the active tab, or nothing, clears the selection of the tab.
        /// <br/><br/>
        /// 활성 탭에서 지정한 노드를 선택합니다. 사용자가 선택한 것처럼 <see cref="selectedNodesChanging"/>과 <see cref="selectedNodesChanged"/>를 발생시킵니다.<br/>
        /// 활성 탭의 현재 폴더를 넘기거나 아무것도 넘기지 않으면 그 탭의 선택을 해제합니다.
        /// </summary>
        /// <param name="nodes">
        /// The nodes to select. Nodes outside the current location of the active tab are ignored.<br/>
        /// 선택할 노드입니다. 활성 탭의 현재 위치 밖에 있는 노드는 무시합니다.
        /// </param>
        public void SetSelectedNodes(IEnumerable<IONode> nodes)
        {
            IONode tabNode = activeTab.node;
            treeView.SetSelection(nodes.Where(x => x.provider == tabNode.provider && x != tabNode).Select(static x => x.path));
        }

        /// <summary>
        /// Rebuilds the navigation pane from the streaming assets locations that exist, and opens the first one if no location is open yet.<br/>
        /// 실제로 있는 StreamingAssets 위치로 탐색 영역을 다시 만들고, 아직 연 위치가 없으면 첫 위치를 엽니다.
        /// </summary>
        /// <returns>
        /// An asynchronous operation that completes when the navigation pane is rebuilt.<br/>
        /// 탐색 영역을 다시 만들면 완료되는 비동기 작업입니다.
        /// </returns>
        public async UniTask RefreshNavigation()
        {
            IOBrowserPlaceGroup streamingAssets = new(IStreamingAssetsService.streamingAssetsFolderName) { name = "streaming-assets" };
            IOBrowserPlaceGroup editorStreamingAssets = new(EditorStreamingAssetsService.editorStreamingAssetsFolderName) { name = "editor-streaming-assets" };

            await AddPlaces(streamingAssets, EditorStreamingAssetsService.sources);
            await AddPlaces(editorStreamingAssets, EditorStreamingAssetsService.editorSources);

            SetNavigationGroups(new[] { streamingAssets, editorStreamingAssets }.Where(static x => x.childCount > 0));

            if (activeTab.node != IONode.empty)
                return;

            if (streamingAssets.Children().Concat(editorStreamingAssets.Children()).FirstOrDefault() is IOBrowserPlace { userData: IONode first })
                activeTab.ResetNode(first);
        }

        async UniTask AddPlaces(IOBrowserPlaceGroup group, IReadOnlyList<EditorStreamingAssetsService.Source> sources)
        {
            foreach (EditorStreamingAssetsService.Source source in sources)
            {
                IONode root = source.provider.rootNode;
                if (!await root.dir.Exists())
                    continue;

                IOBrowserPlace place = new(source.package?.displayName ?? Application.productName, () => node = root)
                {
                    userData = root,
                    dropDirectory = source.provider is IWritableIOProvider ? root : null,
                    iconManipulator =
                    {
                        assetRef = folderIcon.icon,
                        editorDarkAssetRef = folderIcon.editorDarkIcon
                    }
                };

                group.Add(place);
            }
        }

        void OnProjectChanged() => RefreshNavigation().Forget();

        bool CanChange(ImmutableArray<IONode> nodes) => IsSelected(nodes) || (selectedNodesChanging?.GetInvocationList().OfType<Func<ImmutableArray<IONode>, bool>>().All(x => x.Invoke(nodes)) ?? true);

        // selectedNodes를 계산한 위치이며, nodeChanged가 새로 고침인지 다른 위치로의 이동인지 구분합니다.
        IONode selectedNodesDirectory;

        void Accept(IONode directory, ImmutableArray<IONode> nodes)
        {
            selectedNodesDirectory = directory;
            if (IsSelected(nodes))
                return;

            selectedNodes = nodes;
            selectedNodesChanged?.Invoke(nodes);
        }

        // 순서와 관계없이 같은 노드인지 확인합니다.
        bool IsSelected(ImmutableArray<IONode> nodes)
        {
            if (nodes.Length != selectedNodes.Length)
                return false;

            foreach (IONode node in nodes)
            {
                if (!selectedNodes.Contains(node))
                    return false;
            }

            return true;
        }

        ImmutableArray<IONode> GetSelectedNodes(IOBrowserTab tab) => tab is IONodeBrowserTab nodeTab ? GetNodes(tab.node, nodeTab.treeView.selectedEntries) : [];

        // 선택한 항목이 없으면 현재 폴더를 선택한 것으로 봅니다. StreamingAssets 위치 밖의 노드는 넣지 않습니다.
        static ImmutableArray<IONode> GetNodes(IONode directory, ImmutableArray<IOEntry> entries)
        {
            if (!ResourcePackNodeReference.TryFindSource(directory.provider, out _, out _))
                return [];

            if (entries.IsEmpty)
                return [directory];

            return [..entries.Select(directory.Bind)];
        }

        static IONode GetMovedNode(IONode node, ImmutableArray<(IONode source, IONode destination)> moves)
        {
            foreach ((IONode source, IONode destination) in moves)
            {
                if (IsSameOrDescendant(node, source) && node.path.TryGetRelativePath(source.path, out RuniPath relativePath))
                    return destination.CreateInRoot(relativePath.IsEmpty() ? destination.path : destination.path / relativePath);
            }

            return node;
        }

        static bool IsSameOrDescendant(IONode node, IONode ancestor) => node.provider.IsSameTarget(ancestor.provider) && node.path.StartsWith(ancestor.path);
    }
}
