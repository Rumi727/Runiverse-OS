#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    [UxmlElement]
    [Icon("UIToolkit/Icons/VisualElement.png")]
    public sealed partial class FlexibleSpace : VisualElement
    {
        public const string ussClassName = "runios-flexible-space";

        public FlexibleSpace()
        {
            styleSheets.Add(UIElementsUtility.rosControlStyle);
            AddToClassList(ussClassName);
        }
    }
}