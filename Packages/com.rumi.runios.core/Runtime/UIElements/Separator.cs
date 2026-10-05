#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    [UxmlElement]
    public partial class Separator : VisualElement
    {
        public const string ussClassName = "runios-separator";
        public const string horizontalUssClassName = "runios-separator--horizontal";
        public const string verticalUssClassName = "runios-separator--vertical";

        [UxmlAttribute]
        public SeparatorDirection direction
        {
            get;
            set
            {
                field = value;

                EnableInClassList(horizontalUssClassName, direction == SeparatorDirection.horizontal);
                EnableInClassList(verticalUssClassName, direction == SeparatorDirection.vertical);
            }
        }

        public Separator() : this(SeparatorDirection.horizontal) { }

        public Separator(SeparatorDirection direction)
        {
            styleSheets.Add(UIElementsUtility.rosControlStyle);
            AddToClassList(ussClassName);

            this.direction = direction;
        }
    }
}