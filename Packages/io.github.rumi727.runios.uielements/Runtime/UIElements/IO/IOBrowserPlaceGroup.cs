#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IOBrowserPlaceGroup : VisualElement
    {
        public const string ussClassName = "runios-io-browser-place-group";
        public const string headerUssClassName = "runios-io-browser-place-group__header";
        public const string contentUssClassName = "runios-io-browser-place-group__content";

        [UxmlAttribute]
        public string text
        {
            get => header.text;
            set => header.text = value;
        }

        public Label header { get; }

        public VisualElement content { get; }
        public override VisualElement contentContainer => content;

        public IOBrowserPlaceGroup() : this(string.Empty) { }

        public IOBrowserPlaceGroup(string text)
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            header = new Label(text) { name = "header" };
            header.AddToClassList(headerUssClassName);
            hierarchy.Add(header);

            content = new VisualElement { name = "content" };
            content.AddToClassList(contentUssClassName);
            hierarchy.Add(content);
        }
    }
}
