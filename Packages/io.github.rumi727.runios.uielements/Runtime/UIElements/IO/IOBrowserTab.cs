#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// <see cref="IOBrowser"/>의 탭 하나입니다. 탭마다 위치와 뒤로·앞으로 기록, 내용 뷰를 따로 가집니다.
    /// </summary>
    public abstract class IOBrowserTab : Tab, IIODropTarget
    {
        public new const string ussClassName = "runios-io-browser-tab";
        public const string headerDropTargetUssClassName = "runios-io-browser-tab__header--drop-target";

        public virtual IONode node
        {
            get => _node;
            set
            {
                // 같은 위치를 다시 열면 새로고침만 하고 기록하지 않습니다.
                if (_node != value)
                {
                    if (!CanChangeNode(value))
                        return;

                    backHistory.Push(_node);
                    forwardHistory.Clear();
                }

                SetNode(value);
            }
        }
        IONode _node = IONode.empty;

        readonly Stack<IONode> backHistory = new();
        readonly Stack<IONode> forwardHistory = new();

        public bool canGoBack => backHistory.Count > 0;
        public bool canGoForward => forwardHistory.Count > 0;

        /// <summary>
        /// 다른 위치로 이동하기 직전에 발생합니다. 구독자가 하나라도 <see langword="false"/>를 반환하면 이동하지 않습니다.
        /// 새로고침과 <see cref="ResetNode"/>에서는 발생하지 않습니다.
        /// </summary>
        public event Func<IOBrowserTab, IONode, bool>? nodeChanging;

        /// <summary>
        /// 이동하거나 새로고침해서 위치가 다시 정해지면 발생합니다.
        /// </summary>
        public event Action<IOBrowserTab>? nodeChanged;

        /// <summary>
        /// 내용 영역에서 파일을 열면 발생합니다.
        /// </summary>
        public event Action<IONode>? fileOpened;

        protected IOBrowserTab()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            closeable = true;
        }

        public void Back()
        {
            if (!backHistory.TryPeek(out IONode previous) || !CanChangeNode(previous))
                return;

            backHistory.Pop();
            forwardHistory.Push(_node);
            SetNode(previous);
        }

        public void Forward()
        {
            if (!forwardHistory.TryPeek(out IONode next) || !CanChangeNode(next))
                return;

            forwardHistory.Pop();
            backHistory.Push(_node);
            SetNode(next);
        }

        /// <summary>
        /// 뒤로·앞으로 기록을 지우고 지정한 위치에서 새로 시작합니다.
        /// </summary>
        public void ResetNode(IONode value)
        {
            backHistory.Clear();
            forwardHistory.Clear();

            SetNode(value);
        }

        bool CanChangeNode(IONode value) => nodeChanging?.GetInvocationList().OfType<Func<IOBrowserTab, IONode, bool>>().All(x => x.Invoke(this, value)) ?? true;

        void SetNode(IONode value)
        {
            _node = value;

            OnNodeChanged();
            nodeChanged?.Invoke(this);
        }

        /// <summary>
        /// 폴더는 이 탭에서 그 폴더로 이동하고, 파일은 <see cref="fileOpened"/>를 발생시킵니다.
        /// </summary>
        protected void OpenEntry(IOEntry entry)
        {
            IONode entryNode = node.Bind(entry);

            if (entry.isDirectory)
                node = entryNode;
            else
                fileOpened?.Invoke(entryNode);
        }

        /// <summary>
        /// 탭 제목을 현재 위치에 맞게 다시 정합니다.
        /// </summary>
        public void RefreshLabel() => label = GetLabel();

        protected virtual string GetLabel()
        {
            if (string.IsNullOrEmpty(node.name))
                return LocalizationUtility.GetTextOrKey("runios-editor:gui.root");

            return node.name;
        }

        /// <summary>
        /// 이 탭의 내용 뷰에서 항목을 바꾸지 못하게 할지 정합니다. 보통 <see cref="IOBrowser.readOnly"/>가 정합니다.
        /// </summary>
        public abstract bool readOnly { get; set; }

        /// <summary>
        /// 이 탭의 내용 뷰에 표시할 파일을 거릅니다. 폴더에는 적용되지 않으며, <see langword="null"/>이면 모든 파일을 표시합니다. 보통 <see cref="IOBrowser.filter"/>가 정합니다.
        /// </summary>
        public abstract IPatternMatcher? filter { get; set; }

        // 홈 화면(장치 목록)이나 읽기 전용인 탭에는 놓을 수 없습니다.
        bool IIODropTarget.TryGetDropDirectory(Vector2 position, out IONode directory)
        {
            directory = node;
            return node != IONode.empty && !readOnly;
        }

        void IIODropTarget.OnDragOver(IODragData data, Vector2 position, bool canDrop) => tabHeader.EnableInClassList(headerDropTargetUssClassName, canDrop);

        void IIODropTarget.OnDragLeave() => tabHeader.RemoveFromClassList(headerDropTargetUssClassName);

        protected abstract void OnNodeChanged();

        /// <summary>
        /// 현재 위치의 내용을 다시 읽어 옵니다.
        /// </summary>
        public abstract UniTask Refresh();
    }
}
