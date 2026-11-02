#nullable enable
using RuniOS.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IOBrowserPlace : VisualElement, IIODropTarget
    {
        public const string ussClassName = "runios-io-browser-place";
        public const string headerUssClassName = "runios-io-browser-place__header";
        public const string iconUssClassName = "runios-io-browser-place__icon";
        public const string labelUssClassName = "runios-io-browser-place__label";
        public const string dropTargetUssClassName = "runios-io-browser-place--drop-target";

        [UxmlAttribute]
        public string text
        {
            get => label.text;
            set => label.text = value;
        }

        public VisualElement header { get; }

        public Image icon { get; }
        public AssetScopeManipulator<VectorImage> iconManipulator { get; }

        public Label label { get; }

        public Clickable clickable { get; }

        /// <summary>
        /// 이 장소에 IO 항목을 끌어다 놓으면 들어갈 폴더입니다. <see langword="null"/>이면 놓을 수 없습니다.
        /// </summary>
        public IONode? dropDirectory { get; set; }

        public IOBrowserPlace() : this(string.Empty, null) { }

        public IOBrowserPlace(string text, Action? clicked)
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            header = new VisualElement { name = "header" };
            header.AddToClassList(headerUssClassName);
            Add(header);

            icon = new Image { name = "icon" };
            icon.AddToClassList(iconUssClassName);
            iconManipulator = new AssetScopeManipulator<VectorImage>
            {
                applyAsset = x => icon.vectorImage = x,
                clearAsset = () => icon.vectorImage = null
            };
            icon.AddManipulator(iconManipulator);
            header.Add(icon);

            label = new Label(text) { name = "label" };
            label.AddToClassList(labelUssClassName);
            header.Add(label);

            clickable = new Clickable(clicked);
            this.AddManipulator(clickable);
        }

        bool IIODropTarget.TryGetDropDirectory(Vector2 position, out IONode directory)
        {
            directory = dropDirectory ?? default;
            return dropDirectory != null;
        }

        void IIODropTarget.OnDragOver(IODragData data, Vector2 position, bool canDrop) => EnableInClassList(dropTargetUssClassName, canDrop);

        void IIODropTarget.OnDragLeave() => RemoveFromClassList(dropTargetUssClassName);
    }
}
