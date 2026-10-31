#nullable enable
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    /// <summary>
    /// <see cref="IOBrowser"/>의 탭 하나입니다. 탭마다 위치와 뒤로·앞으로 기록, 내용 뷰를 따로 가집니다.
    /// </summary>
    public abstract class IOBrowserTab : Tab
    {
        public virtual IONode node
        {
            get => _node;
            set
            {
                // 같은 위치를 다시 열면 새로고침만 하고 기록하지 않습니다.
                if (_node != value)
                {
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
            closeable = true;
        }

        public void Back()
        {
            if (!backHistory.TryPop(out IONode previous))
                return;

            forwardHistory.Push(_node);
            SetNode(previous);
        }

        public void Forward()
        {
            if (!forwardHistory.TryPop(out IONode next))
                return;

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

        protected abstract void OnNodeChanged();
    }
}
