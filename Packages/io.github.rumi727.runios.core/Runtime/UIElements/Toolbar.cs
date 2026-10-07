#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    [UxmlElement]
    [Icon("UIToolkit/Icons/Toolbar.png")]
    public partial class Toolbar : VisualElement
    {
        public const string ussClassName = "runios-toolbar";

        public Toolbar()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);
        }
    }
}