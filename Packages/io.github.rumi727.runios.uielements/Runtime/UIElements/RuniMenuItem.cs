#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// <see cref="RuniMenu"/>에 넣는 일반적인 드롭다운 항목입니다. 체크, 아이콘, 텍스트, 단축키를 한 줄로 표시합니다.
    /// <br/>
    /// 클릭하거나 Submit하면 <see cref="clicked"/>를 호출하고, <see cref="closeOnClick"/>이면 메뉴를 모두 닫습니다.
    /// </summary>
    [UxmlElement]
    public partial class RuniMenuItem : VisualElement
    {
        public const string ussClassName = "runios-menu-item";
        public const string checkUssClassName = ussClassName + "__check";
        public const string iconUssClassName = ussClassName + "__icon";
        public const string labelUssClassName = ussClassName + "__label";
        public const string shortcutUssClassName = ussClassName + "__shortcut";
        public const string checkedUssClassName = ussClassName + "--checked";
        /// <summary><see cref="iconManipulator"/>로 아이콘이 적용되어 있을 때 붙습니다. 체크와 아이콘은 같은 자리를 나눠 씁니다.</summary>
        public const string hasIconUssClassName = ussClassName + "--has-icon";

        [UxmlAttribute]
        public string text
        {
            get => label.text;
            set => label.text = value;
        }

        [UxmlAttribute]
        public bool isChecked
        {
            get;
            set
            {
                field = value;
                EnableInClassList(checkedUssClassName, value);
            }
        }

        /// <summary>
        /// 표시할 단축키들입니다. 쉼표로 이어 표시하며, 단축키를 눌러도 이 항목이 실행되지는 않습니다.
        /// 배열의 원소를 직접 바꾸면 표시가 갱신되지 않으므로 배열을 다시 할당합니다.
        /// </summary>
        [UxmlAttribute]
        public KeyShortcut[] shortcuts
        {
            get;
            set
            {
                field = value;
                shortcutLabel.text = string.Join(", ", value.Where(static x => x.keyCode != KeyCode.None));
            }
        } = [];

        [UxmlAttribute]
        public bool closeOnClick { get; set; } = true;

        public VisualElement check { get; }

        public Image icon { get; }
        public AssetScopeManipulator<VectorImage> iconManipulator { get; }

        public Label label { get; }
        public Label shortcutLabel { get; }

        public Clickable clickable { get; }

        public event Action? clicked;

        public RuniMenuItem() : this(string.Empty, null) { }

        public RuniMenuItem(string text, Action? clicked)
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            focusable = true;

            check = new VisualElement { name = "check" };
            check.AddToClassList(checkUssClassName);
            Add(check);

            icon = new Image { name = "icon" };
            icon.AddToClassList(iconUssClassName);
            iconManipulator = new AssetScopeManipulator<VectorImage>
            {
                applyAsset = x =>
                {
                    icon.vectorImage = x;
                    AddToClassList(hasIconUssClassName);
                },
                clearAsset = () =>
                {
                    icon.vectorImage = null;
                    RemoveFromClassList(hasIconUssClassName);
                }
            };
            icon.AddManipulator(iconManipulator);
            Add(icon);

            label = new Label(text) { name = "label" };
            label.AddToClassList(labelUssClassName);
            Add(label);

            shortcutLabel = new Label { name = "shortcut" };
            shortcutLabel.AddToClassList(shortcutUssClassName);
            Add(shortcutLabel);

            if (clicked != null)
                this.clicked += clicked;

            clickable = new Clickable(Invoke);
            this.AddManipulator(clickable);

            RegisterCallback<NavigationSubmitEvent>(OnNavigationSubmit);
        }

        void OnNavigationSubmit(NavigationSubmitEvent evt)
        {
            if (evt.target != this)
                return;

            Invoke();
            evt.StopPropagation();
        }

        // GenericDropdownMenu와 같이 동작을 먼저 실행하고 메뉴를 닫습니다.
        void Invoke()
        {
            clicked?.Invoke();

            if (closeOnClick)
                RuniMenu.GetMenu(this)?.CloseAll();
        }
    }
}
