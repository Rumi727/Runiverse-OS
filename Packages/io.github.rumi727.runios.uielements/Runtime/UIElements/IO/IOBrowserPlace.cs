#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements.IO
{
    [UxmlElement]
    public partial class IOBrowserPlace : VisualElement
    {
        public const string ussClassName = "runios-io-browser-place";
        public const string labelUssClassName = "runios-io-browser-place__label";

        [UxmlAttribute]
        public string text
        {
            get => label.text;
            set => label.text = value;
        }

        public Label label { get; }

        public Clickable clickable { get; }

        public IOBrowserPlace() : this(string.Empty, null) { }

        public IOBrowserPlace(string text, Action? clicked)
        {
            AddToClassList(ussClassName);

            label = new Label(text) { name = "label" };
            label.AddToClassList(labelUssClassName);
            Add(label);

            clickable = new Clickable(clicked);
            this.AddManipulator(clickable);
        }
    }
}
