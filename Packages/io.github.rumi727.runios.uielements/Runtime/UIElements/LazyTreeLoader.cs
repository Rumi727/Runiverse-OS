#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// Loads the children of <see cref="BaseTreeView"/> items asynchronously when they are expanded.<br/>
    /// Requires the tree view to use <see cref="LazyTreeViewController{T}"/> or <see cref="LazyMultiColumnTreeViewController{T}"/> created with this loader.
    /// <br/><br/>
    /// <see cref="BaseTreeView"/> 항목이 펼쳐질 때 자식을 비동기로 로드합니다.<br/>
    /// 트리 뷰는 이 로더로 생성한 <see cref="LazyTreeViewController{T}"/> 또는 <see cref="LazyMultiColumnTreeViewController{T}"/>를 사용해야 합니다.
    /// </summary>
    public class LazyTreeLoader<T>
    {
        const int rootId = -1;

        public LazyTreeLoader(BaseTreeView treeView, Func<T, bool> mayHaveChildren, Func<T, IUniTaskAsyncEnumerable<T>> getChildren)
        {
            this.treeView = treeView;
            this.mayHaveChildren = mayHaveChildren;
            this.getChildren = getChildren;

            treeView.itemExpandedChanged += OnItemExpandedChanged;

            // 일회성 예약 항목은 실행 후 비활성화되며, 활성 상태에서의 Resume은 아무것도 하지 않습니다.
            // 따라서 Resume을 여러 번 호출해도 다음 스케줄러 틱에 한 번만 새로고침됩니다.
            refresh = treeView.schedule.Execute(treeView.RefreshItems);
            refresh.Pause();
        }

        public BaseTreeView treeView { get; }

        /// <summary>
        /// Gets or sets how many levels below an expanded item are loaded in advance.<br/>
        /// <c>0</c> loads only the children of the expanded item; <c>1</c> also loads the children of those children.
        /// <br/><br/>
        /// 펼쳐진 항목 아래 몇 단계까지 미리 로드할지 가져오거나 설정합니다.<br/>
        /// <c>0</c>은 펼쳐진 항목의 자식만 로드하고, <c>1</c>은 그 자식들의 자식까지 함께 로드합니다.
        /// </summary>
        public int preloadDepth { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of child loads that run at the same time.<br/>
        /// Loads requested by expanding or binding an item run before preloads, and each group runs in most-recently-requested order.
        /// <br/><br/>
        /// 동시에 실행되는 자식 로드의 최대 개수를 가져오거나 설정합니다.<br/>
        /// 항목을 펼치거나 바인딩해서 요청된 로드가 미리 로드보다 먼저 실행되며, 각각은 가장 최근에 요청된 순서로 실행됩니다.
        /// </summary>
        public int maxConcurrentLoads { get; set; } = 1;

        readonly Func<T, bool> mayHaveChildren;
        readonly Func<T, IUniTaskAsyncEnumerable<T>> getChildren;

        // 로드 중인 항목과, 로드가 끝난 뒤 그 자식들을 미리 로드할 깊이입니다.
        readonly Dictionary<int, int> loadingDepths = [];
        readonly HashSet<int> loadedIds = [];

        readonly LinkedList<int> pendingRequestedIds = new();
        readonly LinkedList<int> pendingPreloadIds = new();
        int runningLoadCount;

        readonly IVisualElementScheduledItem refresh;

        CancellationTokenSource cancellation = new();
        int nextId;

        /// <summary>
        /// Clears the tree and loads the specified root items.<br/>
        /// 트리를 비우고 지정한 루트 항목을 로드합니다.
        /// </summary>
        public UniTask Reset(IUniTaskAsyncEnumerable<T> roots)
        {
            cancellation.Cancel();
            cancellation.Dispose();
            cancellation = new CancellationTokenSource();

            loadingDepths.Clear();
            loadedIds.Clear();

            // 취소된 이전 로드는 실행 수를 반환하지 않으므로 여기서 초기화합니다.
            pendingRequestedIds.Clear();
            pendingPreloadIds.Clear();
            runningLoadCount = 0;

            treeView.SetRootItems(new List<TreeViewItemData<T>>());

            // 루트 목록은 항상 펼쳐진 것처럼 보이므로 펼친 항목과 같은 깊이로 미리 로드합니다.
            loadingDepths.Add(rootId, preloadDepth);
            return Load(rootId, () => roots, cancellation.Token);
        }

        /// <summary>
        /// Returns whether the item should be treated as having children.<br/>
        /// Until the item is loaded, <paramref name="hasLoadedChildren"/> is supplemented by the may-have-children hint.
        /// <br/><br/>
        /// 항목을 자식이 있는 것으로 취급할지 반환합니다.<br/>
        /// 항목이 로드되기 전에는 <paramref name="hasLoadedChildren"/>에 자식 존재 가능성 힌트를 더합니다.
        /// </summary>
        public bool HasChildren(int id, T item, bool hasLoadedChildren)
        {
            if (hasLoadedChildren || loadedIds.Contains(id))
                return hasLoadedChildren;

            return mayHaveChildren.Invoke(item);
        }

        /// <summary>
        /// Returns whether the children of the item are being loaded.<br/>
        /// 항목의 자식을 로드하는 중인지 반환합니다.
        /// </summary>
        public bool IsLoading(int id) => loadingDepths.ContainsKey(id);

        /// <summary>
        /// Returns whether the children of the item have been loaded.<br/>
        /// 항목의 자식 로드가 완료되었는지 반환합니다.
        /// </summary>
        public bool IsLoaded(int id) => loadedIds.Contains(id);

        /// <summary>
        /// Starts loading the children of the item if they are not loaded or being loaded yet, and preloads below them up to <see cref="preloadDepth"/>.<br/>
        /// 항목의 자식이 아직 로드되지 않았고 로드 중도 아니면 로드를 시작하고, 그 아래를 <see cref="preloadDepth"/>만큼 미리 로드합니다.
        /// </summary>
        public void LoadChildren(int id) => LoadChildren(id, preloadDepth, true);

        void LoadChildren(int id, int depth, bool isRequested)
        {
            if (loadedIds.Contains(id))
            {
                // 미리 로드된 항목을 펼친 경우, 그 아래도 같은 깊이만큼 미리 로드해야 합니다.
                PreloadChildren(treeView.viewController.GetChildrenIds(id), depth);
                return;
            }

            if (loadingDepths.TryGetValue(id, out int loadingDepth))
            {
                // 미리 로드 중인 항목을 펼친 경우, 로드가 끝난 뒤 더 깊이 미리 로드해야 할 수 있습니다.
                loadingDepths[id] = Math.Max(loadingDepth, depth);

                // 아직 대기 중이라면 가장 최근 요청으로서 먼저 실행하고, 사용자가 요청한 미리 로드는 요청 대기열로 옮깁니다.
                if (pendingRequestedIds.Remove(id) || (isRequested && pendingPreloadIds.Remove(id)))
                    pendingRequestedIds.AddFirst(id);
                else if (pendingPreloadIds.Remove(id))
                    pendingPreloadIds.AddFirst(id);

                return;
            }

            T item = treeView.GetItemDataForId<T>(id);
            if (!mayHaveChildren.Invoke(item))
                return;

            loadingDepths.Add(id, depth);

            if (isRequested)
                pendingRequestedIds.AddFirst(id);
            else
                pendingPreloadIds.AddFirst(id);

            // 펼치기와 바인딩은 로딩 상태가 기록되기 전에 행을 그리므로, 로딩 표시가 보이도록 새로고침을 예약합니다.
            refresh.Resume();

            StartPendingLoads();
        }

        void StartPendingLoads()
        {
            while (runningLoadCount < maxConcurrentLoads)
            {
                LinkedList<int> pendingIds = pendingRequestedIds.Count > 0 ? pendingRequestedIds : pendingPreloadIds;
                if (pendingIds.First is not { } first)
                    return;

                pendingIds.RemoveFirst();
                runningLoadCount++;

                int id = first.Value;
                T item = treeView.GetItemDataForId<T>(id);

                // 바인딩 도중 RefreshItems가 재진입하지 않도록 다음 스케줄러 틱에서 로드합니다.
                CancellationToken cancellationToken = cancellation.Token;
                treeView.schedule.Execute(() => RunLoad(id, item, cancellationToken).Forget());
            }
        }

        async UniTask RunLoad(int id, T item, CancellationToken cancellationToken)
        {
            try
            {
                await Load(id, () => getChildren.Invoke(item), cancellationToken);
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    runningLoadCount--;
                    StartPendingLoads();
                }
            }
        }

        /// <summary>
        /// Loads the children of an item that is bound while already expanded, such as by Alt+click recursive expansion.<br/>
        /// Alt+클릭 재귀 펼치기처럼 이미 펼쳐진 상태로 바인딩된 항목의 자식을 로드합니다.
        /// </summary>
        public void OnBindItem(int id)
        {
            // 이미 로드된 항목은 펼칠 때 처리되므로, 바인딩마다 자식을 다시 순회하지 않습니다.
            if (treeView.IsExpanded(id) && !loadedIds.Contains(id))
                LoadChildren(id);
        }

        void PreloadChildren(IEnumerable<int> childIds, int depth)
        {
            if (depth <= 0)
                return;

            // 대기열 앞에 쌓이므로, 역순으로 요청해야 자식 순서대로 로드됩니다.
            foreach (int childId in childIds.Reverse())
                LoadChildren(childId, depth - 1, false);
        }

        void OnItemExpandedChanged(TreeViewExpansionChangedArgs args)
        {
            if (args.isExpanded)
                LoadChildren(args.id);
            else
                OnItemCollapsed(args.id);
        }

        void OnItemCollapsed(int id)
        {
            // 접힌 항목 아래는 보이지 않으므로, 아직 시작하지 않은 로드는 대기열에서 빼고 로드 전 상태로 되돌립니다.
            // 실행 중인 로드는 자식이 일부만 들어간 상태를 남기지 않도록 끝까지 진행합니다.
            RemovePendingDescendants(pendingRequestedIds, id);
            RemovePendingDescendants(pendingPreloadIds, id);

            // 접힌 항목 자신은 여전히 보이므로 로드는 유지하되, 펼쳐서 늘어난 미리 로드 깊이는 부모 아래 항목으로서의 깊이로 되돌립니다.
            if (loadingDepths.TryGetValue(id, out int depth))
                loadingDepths[id] = Math.Min(depth, preloadDepth - 1);
        }

        void RemovePendingDescendants(LinkedList<int> pendingIds, int ancestorId)
        {
            LinkedListNode<int>? node = pendingIds.First;
            while (node != null)
            {
                LinkedListNode<int>? next = node.Next;
                if (IsDescendant(node.Value, ancestorId))
                {
                    pendingIds.Remove(node);
                    loadingDepths.Remove(node.Value);
                }

                node = next;
            }
        }

        bool IsDescendant(int id, int ancestorId)
        {
            BaseTreeViewController viewController = treeView.viewController;
            for (int parentId = viewController.GetParentId(id); parentId != rootId; parentId = viewController.GetParentId(parentId))
            {
                if (parentId == ancestorId)
                    return true;
            }

            return false;
        }

        // 자식 열거 생성이 예외를 던져도 로드 완료 처리가 되도록, 열거 생성까지 try 안에서 수행합니다.
        async UniTask Load(int parentId, Func<IUniTaskAsyncEnumerable<T>> enumerateChildren, CancellationToken cancellationToken)
        {
            List<int> childIds = [];

            try
            {
                await foreach (T child in enumerateChildren.Invoke().WithCancellation(cancellationToken))
                {
                    // 소스가 취소를 무시하더라도 Reset 이후의 트리에 이전 항목이 추가되면 안 됩니다.
                    cancellationToken.ThrowIfCancellationRequested();

                    int childId = nextId++;
                    treeView.AddItem<T>(new TreeViewItem<T>(childId, child), parentId, rebuildTree: false);
                    childIds.Add(childId);

                    // 열거가 끝나기 전에도 지금까지 받은 항목이 보이도록 새로고침을 예약합니다.
                    refresh.Resume();
                }
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    loadingDepths.Remove(parentId, out int depth);
                    loadedIds.Add(parentId);

                    // 빈 디렉터리의 펼침 화살표처럼 로드 완료 상태에 따라 바뀌는 표시를 반영합니다.
                    refresh.Resume();

                    PreloadChildren(childIds, depth);
                }
            }
        }
    }
}
