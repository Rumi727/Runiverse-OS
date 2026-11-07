#nullable enable
using RuniOS.Editor.Localizations;
using RuniOS.Editor.UIElements.Resource;
using RuniOS.IO;
using RuniOS.UIElements.IO;
using System.Collections.Immutable;

namespace RuniOS.Editor.Resource
{
    /// <summary>
    /// Shows a <see cref="ResourcePackBrowser"/> and reports its selection and the changes made in it to other windows.<br/>
    /// <see cref="ResourcePackBrowser"/>를 보여주고, 그 선택과 그 안에서 한 변경을 다른 창에 알립니다.
    /// </summary>
    /// <remarks>
    /// The locations, the selection of every tab, and the active tab are restored after the domain reloads. The back and forward history is not restored.<br/>
    /// 도메인을 다시 불러온 뒤에 모든 탭의 위치와 선택, 활성 탭을 되살립니다. 뒤로·앞으로 기록은 되살리지 않습니다.
    /// </remarks>
    public sealed class ResourcePackBrowserWindow : EditorWindow, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Gets <see cref="ResourcePackBrowser.selectedNodes"/> of the open browser window.<br/>
        /// 열린 브라우저 창의 <see cref="ResourcePackBrowser.selectedNodes"/>를 가져옵니다.
        /// </summary>
        public static ImmutableArray<IONode> selection { get; private set; } = [];

        /// <inheritdoc cref="ResourcePackBrowser.selectedNodesChanging"/>
        public static event Func<ImmutableArray<IONode>, bool>? selectionChanging;

        /// <inheritdoc cref="ResourcePackBrowser.selectedNodesChanged"/>
        public static event Action<ImmutableArray<IONode>>? selectionChanged;

        /// <summary>
        /// Occurs before items are moved in the browser, with the items to move. If any subscriber returns <see langword="false"/>, the items are not moved.<br/>
        /// 브라우저에서 항목을 옮기기 전에 옮길 항목과 함께 발생합니다. 구독자가 하나라도 <see langword="false"/>를 반환하면 옮기지 않습니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.moving"/>
        public static event Func<ImmutableArray<IONode>, bool>? moving;

        /// <summary>
        /// Occurs after items are moved in the browser, with the source and destination of each moved item.<br/>
        /// 브라우저에서 항목을 옮긴 뒤에 옮긴 항목마다 원본과 대상과 함께 발생합니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.moved"/>
        public static event Action<ImmutableArray<(IONode source, IONode destination)>>? moved;

        /// <summary>
        /// Occurs before items are deleted or overwritten in the browser, with the items that will disappear. If any subscriber returns <see langword="false"/>, they are kept.<br/>
        /// 브라우저에서 항목을 지우거나 덮어쓰기 전에 사라질 항목과 함께 발생합니다. 구독자가 하나라도 <see langword="false"/>를 반환하면 남겨 둡니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.deleting"/>
        public static event Func<ImmutableArray<IONode>, bool>? deleting;

        /// <summary>
        /// Occurs after items are deleted or overwritten in the browser, with the items that disappeared or were replaced.<br/>
        /// 브라우저에서 항목을 지우거나 덮어쓴 뒤에 사라지거나 대체된 항목과 함께 발생합니다.
        /// </summary>
        /// <seealso cref="IONodeMultiColumnTreeView.deleted"/>
        public static event Action<ImmutableArray<IONode>>? deleted;

        [Serializable]
        struct TabState
        {
            public ResourcePackNodeReference node;
            public List<string> selectedPaths;
        }

        [SerializeField] List<TabState> tabStates = [];
        [SerializeField] int activeTabIndex;

        ResourcePackBrowser? browser;

        // ReSharper disable once Unity.IncorrectMethodSignature
        [MenuItem("RuniOS/Resource Pack Browser")]
        public static ResourcePackBrowserWindow GetWindow() => GetWindow<ResourcePackBrowserWindow>();

        /// <summary>
        /// Selects the specified nodes in the browser window that <see cref="selection"/> comes from. Does nothing if no browser window is open.<br/>
        /// <see cref="selection"/>을 알린 브라우저 창에서 지정한 노드를 선택합니다. 열린 브라우저 창이 없으면 아무것도 하지 않습니다.
        /// </summary>
        /// <param name="nodes">
        /// The nodes to select.<br/>
        /// 선택할 노드입니다.
        /// </param>
        /// <seealso cref="ResourcePackBrowser.SetSelectedNodes"/>
        public static void SetSelection(IEnumerable<IONode> nodes)
        {
            // selection은 마지막으로 선택을 알린 창의 selectedNodes와 같은 배열입니다.
            foreach (ResourcePackBrowserWindow window in Resources.FindObjectsOfTypeAll<ResourcePackBrowserWindow>())
            {
                if (window.browser != null && window.browser.selectedNodes == selection)
                {
                    window.browser.SetSelectedNodes(nodes);
                    return;
                }
            }
        }

        void OnEnable()
        {
            EditorLocalization.onLanguageUpdate += UpdateTitle;
            UpdateTitle();
        }

        void OnDisable()
        {
            EditorLocalization.onLanguageUpdate -= UpdateTitle;

            // 창을 닫으면 선택할 수 있는 브라우저가 없으므로 선택을 비웁니다. 다른 창에는 알리지 않습니다.
            selection = [];
        }

        void UpdateTitle() => titleContent = new GUIContent(GetTextOrKey("runios-editor:resource_pack_browser.title"));

        void CreateGUI()
        {
            browser = new ResourcePackBrowser { style = { flexGrow = 1 } };
            rootVisualElement.Add(browser);

            Restore(browser);
            selection = browser.selectedNodes;

            // 되살리는 동안의 변화가 다른 창에 알려지지 않도록, 되살린 뒤에 구독합니다.
            browser.selectedNodesChanging += x => selectionChanging?.GetInvocationList().OfType<Func<ImmutableArray<IONode>, bool>>().All(y => y.Invoke(x)) ?? true;
            browser.selectedNodesChanged += x =>
            {
                selection = x;
                selectionChanged?.Invoke(x);
            };

            browser.moving += (_, x) => moving?.GetInvocationList().OfType<Func<ImmutableArray<IONode>, bool>>().All(y => y.Invoke(x)) ?? true;
            browser.moved += (_, x) => moved?.Invoke(x);
            browser.deleting += (_, x) => deleting?.GetInvocationList().OfType<Func<ImmutableArray<IONode>, bool>>().All(y => y.Invoke(x)) ?? true;
            browser.deleted += (_, x) => deleted?.Invoke(x);
        }

        // 탭을 저장한 순서대로 만들고, 마지막에 저장해 둔 활성 탭을 활성화합니다. 위치를 더 이상 찾을 수 없는 탭은 만들지 않습니다.
        void Restore(ResourcePackBrowser browser)
        {
            IOBrowserTab? activeTab = null;
            ImmutableArray<IONode> activeSelection = [];
            bool isFirstTab = true;

            for (int i = 0; i < tabStates.Count; i++)
            {
                if (!tabStates[i].node.TryResolve(out IONode node))
                    continue;

                // 브라우저는 처음부터 탭 하나를 가지고 있으므로, 첫 탭은 그 탭에 되살립니다.
                IOBrowserTab tab;
                if (isFirstTab)
                {
                    tab = browser.activeTab;
                    tab.ResetNode(node);

                    isFirstTab = false;
                }
                else
                    tab = browser.AddTab(node);

                List<string> selectedPaths = tabStates[i].selectedPaths ?? [];
                ((IONodeBrowserTab)tab).treeView.SetSelectionWithoutNotify(selectedPaths.Select(static x => RuniPath.From(x)));

                if (i == activeTabIndex)
                {
                    activeTab = tab;
                    activeSelection = [..selectedPaths.Select(x => node.CreateInRoot(RuniPath.From(x)))];
                }
            }

            if (activeTab == null)
                return;

            browser.contentPane.activeTab = activeTab;

            // 아직 불러오지 않은 항목은 트리가 불러오는 대로 조용히 선택되므로, 브라우저의 선택도 직접 정해 둡니다.
            browser.SetSelectedNodesWithoutNotify(activeSelection);
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (browser == null)
                return;

            tabStates.Clear();
            activeTabIndex = 0;

            foreach (IOBrowserTab tab in browser.tabs)
            {
                if (!ResourcePackNodeReference.TryCreate(tab.node, out ResourcePackNodeReference reference))
                    continue;

                if (tab == browser.activeTab)
                    activeTabIndex = tabStates.Count;

                tabStates.Add(new TabState
                {
                    node = reference,
                    selectedPaths = [..((IONodeBrowserTab)tab).treeView.selectedEntries.Select(static x => x.path.value)]
                });
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() { }
    }
}
