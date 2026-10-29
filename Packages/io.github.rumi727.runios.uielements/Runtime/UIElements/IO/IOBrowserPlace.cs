#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IOBrowserPlace : VisualElement
    {
        public const string ussClassName = "runios-io-browser-place";
        public const string headerUssClassName = "runios-io-browser-place__header";
        public const string iconUssClassName = "runios-io-browser-place__icon";
        public const string labelUssClassName = "runios-io-browser-place__label";

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

        public IOBrowserPlace() : this(string.Empty, null) { }

        public IOBrowserPlace(string text, Action? clicked)
        {
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
    }
}
