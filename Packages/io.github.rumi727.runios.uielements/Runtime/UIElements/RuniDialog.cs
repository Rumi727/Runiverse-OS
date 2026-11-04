#nullable enable
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// 패널 전체를 덮는 오버레이 위에 띄우는 모달 대화 상자입니다. 제목, 메시지, 추가 내용, 버튼 줄로 이루어집니다.
    /// <br/>
    /// <see cref="ShowAsync"/>로 띄우고 누른 버튼을 받습니다. <see cref="AddButton"/>으로 넣은 버튼을 누르면 그 버튼으로 닫히고,
    /// Esc나 바깥 클릭(<see cref="dismissOnOverlayClick"/>)으로 닫히면 <see langword="null"/>입니다.
    /// <br/>
    /// 대화 상자는 항상 <see cref="overlay"/> 안에 있고, 띄울 때 오버레이만 패널 최상위에 붙입니다.
    /// 페이드 같은 전환은 <see cref="overlayUssClassName"/>에 transition을 선언하고 <see cref="overlayOpenUssClassName"/>에 목표 값을 두면 됩니다.
    /// 오버레이는 전환이 끝난 뒤에 떼어집니다.
    /// </summary>
    public class RuniDialog : VisualElement
    {
        public const string ussClassName = "runios-dialog";
        /// <summary><see cref="title"/>이 비어 있을 때 붙습니다.</summary>
        public const string noTitleUssClassName = ussClassName + "--no-title";
        /// <summary><see cref="message"/>가 비어 있을 때 붙습니다.</summary>
        public const string noMessageUssClassName = ussClassName + "--no-message";
        public const string overlayUssClassName = ussClassName + "__overlay";
        /// <summary>대화 상자가 열려 있는 동안 붙습니다. 처음 배치된 뒤에 붙고, 닫기를 시작하자마자 떨어집니다.</summary>
        public const string overlayOpenUssClassName = overlayUssClassName + "--open";
        public const string titleUssClassName = ussClassName + "__title";
        public const string messageUssClassName = ussClassName + "__message";
        public const string contentUssClassName = ussClassName + "__content";
        public const string buttonsUssClassName = ussClassName + "__buttons";
        public const string buttonUssClassName = ussClassName + "__button";
        /// <summary><see cref="defaultButton"/>에 붙습니다.</summary>
        public const string defaultButtonUssClassName = buttonUssClassName + "--default";

        public string title
        {
            get => titleLabel.text;
            set
            {
                titleLabel.text = value;
                EnableInClassList(noTitleUssClassName, string.IsNullOrEmpty(value));
            }
        }

        public string message
        {
            get => messageLabel.text;
            set
            {
                messageLabel.text = value;
                EnableInClassList(noMessageUssClassName, string.IsNullOrEmpty(value));
            }
        }

        /// <summary>
        /// 대화 상자 바깥(오버레이)을 누르면 <see langword="null"/>로 닫을지 여부입니다.
        /// </summary>
        public bool dismissOnOverlayClick { get; set; }

        /// <summary>
        /// 열 때 포커스를 받는 버튼입니다. <see langword="null"/>이면 첫 번째로 포커스할 수 있는 요소가 받습니다.
        /// Enter는 이 버튼이 아니라 포커스가 있는 버튼을 누릅니다.
        /// </summary>
        public Button? defaultButton
        {
            get;
            set
            {
                field?.RemoveFromClassList(defaultButtonUssClassName);
                field = value;
                field?.AddToClassList(defaultButtonUssClassName);
            }
        }

        public VisualElement overlay { get; }
        public Label titleLabel { get; }
        public Label messageLabel { get; }

        /// <summary>
        /// 메시지와 버튼 사이에 추가 요소를 넣는 자리입니다.
        /// </summary>
        public VisualElement content { get; }

        /// <summary>
        /// 버튼 줄입니다. 여기에 직접 넣은 요소는 대화 상자를 닫지 않으므로, 닫는 버튼은 <see cref="AddButton"/>으로 넣습니다.
        /// </summary>
        public VisualElement buttonContainer { get; }

        public bool isOpen { get; private set; }

        /// <summary>
        /// 대화 상자를 띄우기 직전, 배치되기 전에 발생합니다.
        /// </summary>
        public event Action? opened;

        /// <summary>
        /// 닫기를 시작하면 발생합니다. 오버레이는 전환이 끝난 뒤에 떼어집니다.
        /// </summary>
        public event Action? closed;

        readonly TransitionManipulator overlayTransitions = new();

        // 대화 상자 안에서만 Tab 포커스를 돌리는 데 씁니다. 루트 자신은 링에 들어가지 않습니다.
        readonly VisualElementFocusRing focusRing;

        VisualElement? targetElement;
        Focusable? previousFocus;
        UniTaskCompletionSource<Button?>? resultSource;
        CancellationTokenSource? closingCancellation;

        public RuniDialog() : this(string.Empty, string.Empty) { }

        public RuniDialog(string title, string message)
        {
            AddToClassList(ussClassName);

            // 포커스할 수 있는 자식이 없을 때도 키보드 입력을 받도록 대화 상자 자신이 포커스를 받을 수 있게 합니다.
            // ReSharper disable once VirtualMemberCallInConstructor
            focusable = true;

            overlay = new VisualElement { name = "overlay" };
            overlay.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            overlay.AddToClassList(overlayUssClassName);
            overlay.AddManipulator(overlayTransitions);
            overlay.RegisterCallback<PointerDownEvent>(OnOverlayPointerDown, TrickleDown.TrickleDown);
            overlay.RegisterCallback<GeometryChangedEvent>(OnOverlayGeometryChanged);
            overlay.Add(this);

            titleLabel = new Label { name = "title" };
            titleLabel.AddToClassList(titleUssClassName);
            Add(titleLabel);

            messageLabel = new Label { name = "message" };
            messageLabel.AddToClassList(messageUssClassName);
            Add(messageLabel);

            content = new VisualElement { name = "content" };
            content.AddToClassList(contentUssClassName);
            Add(content);

            buttonContainer = new VisualElement { name = "buttons" };
            buttonContainer.AddToClassList(buttonsUssClassName);
            Add(buttonContainer);

            this.title = title;
            this.message = message;

            focusRing = new VisualElementFocusRing(this);

            RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            RegisterCallback<NavigationCancelEvent>(OnNavigationCancel);
        }

        /// <summary>
        /// 버튼 줄 끝에 누르면 그 버튼으로 대화 상자를 닫는 버튼을 추가합니다.
        /// </summary>
        public Button AddButton(string text)
        {
            Button button = new() { text = text };
            button.AddToClassList(buttonUssClassName);
            button.clicked += () => Close(button);
            buttonContainer.Add(button);

            return button;
        }

        /// <summary>
        /// 대화 상자를 띄우고 닫힐 때까지 기다립니다. 대화 상자는 <paramref name="targetElement"/>가 있는 패널에 표시됩니다.
        /// 이미 열려 있으면 이전 대기를 <see langword="null"/>로 끝내고 다시 엽니다.
        /// </summary>
        /// <param name="targetElement">대화 상자를 표시할 패널을 정하는 요소입니다. 패널에서 떨어지면 대화 상자가 <see langword="null"/>로 닫힙니다.</param>
        /// <returns>닫은 버튼입니다. Esc, 바깥 클릭, <see cref="Close"/>의 기본값 등으로 닫히면 <see langword="null"/>입니다. 닫기를 시작하는 순간 완료됩니다.</returns>
        public UniTask<Button?> ShowAsync(VisualElement targetElement)
        {
            if (targetElement.panel is not { } panel)
                return UniTask.FromResult<Button?>(null);

            // 다시 열 때는 처음 열기 전에 포커스가 있던 요소를 그대로 돌려받습니다.
            if (isOpen)
            {
                Focusable? previous = previousFocus;
                Close(null, false);
                previousFocus = previous;
            }

            // 닫히는 전환을 기다리는 중이면, 오버레이를 다시 붙이다가 그 대기가 끝나 오버레이가 떼어지지 않도록 먼저 취소합니다.
            closingCancellation?.Cancel();
            closingCancellation = null;

            this.targetElement = targetElement;
            targetElement.RegisterCallback<DetachFromPanelEvent>(OnTargetDetach);

            UniTaskCompletionSource<Button?> source = new();
            resultSource = source;

            isOpen = true;
            opened?.Invoke();

            overlay.pickingMode = PickingMode.Position;

            bool wasAttached = overlay.parent == panel.visualTree;
            if (wasAttached)
                overlay.BringToFront();
            else
                panel.visualTree.Add(overlay);

            // 다시 열 때 이미 붙어 있으면 레이아웃이 바뀌지 않아 GeometryChangedEvent가 오지 않으므로 바로 처리합니다.
            if (wasAttached)
                OnPlaced();

            return source.Task;
        }

        /// <summary>
        /// 대화 상자를 닫고, <see cref="ShowAsync"/>의 대기를 <paramref name="result"/>로 끝냅니다.
        /// </summary>
        public void Close(Button? result = null) => Close(result, true);

        void Close(Button? result, bool giveFocusBack)
        {
            if (!isOpen)
                return;

            isOpen = false;
            overlay.RemoveFromClassList(overlayOpenUssClassName);
            overlay.pickingMode = PickingMode.Ignore;

            if (targetElement != null)
            {
                targetElement.UnregisterCallback<DetachFromPanelEvent>(OnTargetDetach);
                targetElement = null;
            }

            // 오버레이가 떼어지면 대화 상자 안의 포커스가 사라지므로, 열기 전에 포커스가 있던 요소로 돌려줍니다.
            Focusable? focused = focusController?.focusedElement;
            bool dialogHasFocus = focused == this || (focused is VisualElement focusedElement && Contains(focusedElement));

            Focusable? previous = previousFocus;
            previousFocus = null;

            if (giveFocusBack && previous is VisualElement previousElement && previousElement.panel == overlay.panel && previous.canGrabFocus)
                previous.Focus();
            else if (dialogHasFocus)
                focused?.Blur();

            // 결과를 기다리던 쪽은 TrySetResult 안에서 바로 이어 실행되므로, 거기서 다시 열어도 이 제거 대기가 취소되도록 먼저 시작합니다.
            RemoveOverlayAfterTransitions().Forget();

            UniTaskCompletionSource<Button?>? source = resultSource;
            resultSource = null;

            closed?.Invoke();
            source?.TrySetResult(result);
        }

        // 스타일이 한 번 계산된 뒤에 overlayOpenUssClassName을 붙여야 여는 전환이 실행됩니다.
        // 포커스도 여기서 옮깁니다. 메뉴 항목에서 띄운 경우, 메뉴가 닫히면서 돌려주는 포커스보다 뒤에 와야 하기 때문입니다.
        void OnPlaced()
        {
            if (!isOpen)
                return;

            overlay.AddToClassList(overlayOpenUssClassName);

            Focusable? focused = focusController?.focusedElement;
            if (focused is not VisualElement focusedElement || !overlay.Contains(focusedElement))
                previousFocus = focused;

            Focusable? first = defaultButton is { canGrabFocus: true } ? defaultButton : focusRing.GetNextFocusable(null, VisualElementFocusChangeDirection.right);
            (first ?? this).Focus();
        }

        void OnOverlayGeometryChanged(GeometryChangedEvent evt)
        {
            // overlayOpenUssClassName이 아직 없으면 패널에 붙은 뒤 처음 배치되는 것입니다.
            if (isOpen && !overlay.ClassListContains(overlayOpenUssClassName))
                OnPlaced();
        }

        async UniTaskVoid RemoveOverlayAfterTransitions()
        {
            CancellationTokenSource cancellation = new();
            closingCancellation = cancellation;

            bool canceled = await overlayTransitions.WaitForTransitions(cancellation.Token).SuppressCancellationThrow();
            cancellation.Dispose();

            if (canceled)
                return;

            closingCancellation = null;
            overlay.RemoveFromHierarchy();
        }

        // Tab은 패널 전체의 포커스 링을 따라 대화 상자 밖으로 나가므로, 대화 상자 안에서만 돌리고 기본 이동을 막습니다.
        // 포커스가 대화 상자 밖으로 나갈 때 되돌리는 방식은 쓰지 않습니다. 대화 상자 안에서 연 드롭다운이나 RuniMenu의 팝업은
        // 대화 상자 밖의 오버레이에 있어서 그 포커스를 빼앗기 때문입니다.
        void OnNavigationMove(NavigationMoveEvent evt)
        {
            FocusChangeDirection direction;
            if (evt.direction == NavigationMoveEvent.Direction.Next)
                direction = VisualElementFocusChangeDirection.right;
            else if (evt.direction == NavigationMoveEvent.Direction.Previous)
                direction = VisualElementFocusChangeDirection.left;
            else
                return;

            // 대화 상자 자신은 링에 없으므로, 자신이 포커스를 가졌으면 처음(또는 끝)부터 찾습니다.
            Focusable? focused = focusController?.focusedElement;
            focusRing.GetNextFocusable(focused == this ? null : focused, direction)?.Focus();

            focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
        }

        void OnNavigationCancel(NavigationCancelEvent evt)
        {
            Close();
            evt.StopPropagation();
        }

        void OnOverlayPointerDown(PointerDownEvent evt)
        {
            if (evt.target == overlay && dismissOnOverlayClick)
            {
                // 이 클릭이 나중에 포커스를 해제해 돌려준 포커스를 잃지 않도록 무시시킵니다.
                // 전환이 없으면 Close 안에서 오버레이가 떼어져 focusController가 null이 되므로 먼저 받아 둡니다.
                FocusController? controller = focusController;
                Close();
                controller?.IgnoreEvent(evt);
                evt.StopPropagation();
                return;
            }

            // FocusController는 누른 요소 자신이 포커스할 수 없으면 포커스를 해제하므로, Esc와 Tab이 대화 상자에 닿도록 지금 포커스를 유지합니다.
            // 자식이 PointerDownEvent를 멈춰도 확인할 수 있도록 TrickleDown에서 받습니다.
            if (evt.target is Focusable { focusable: false })
                focusController?.IgnoreEvent(evt);
        }

        void OnTargetDetach(DetachFromPanelEvent evt)
        {
            if (evt.target == targetElement)
                Close(null, false);
        }
    }
}
