#nullable enable
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// ToolbarGroup groups related controls within a Toolbar.<br/>
    /// It does not impose any particular child control type or interaction behavior.
    /// </summary>
    [UxmlElement]
    [Icon("UIToolkit/Icons/Toolbar.png")]
    public partial class ToolbarGroup : VisualElement
    {
        public const string ussClassName = "runios-toolbar-group";
        public const string firstChildUssClassName = ussClassName + "__first-child";
        public const string middleChildUssClassName = ussClassName + "__middle-child";
        public const string lastChildUssClassName = ussClassName + "__last-child";
        public const string onlyChildUssClassName = ussClassName + "__only-child";
        public const string oddChildUssClassName = ussClassName + "__odd-child";
        public const string evenChildUssClassName = ussClassName + "__even-child";

        public ToolbarGroup()
        {
            this.AddManipulator(new DefaultStyleManipulator(UIElementsUtility.rosControlStyle, UIElementsUtility.rosEditorTheme));
            AddToClassList(ussClassName);

            this.AddManipulator(new ChildrenChangedManipulator(UpdateChildClasses));
        }

        public VisualElement? firstChild { get; private set; }
        public VisualElement? lastChild { get; private set; }
        public VisualElement? onlyChild { get; private set; }

        readonly List<VisualElement> previousChildren = [];

        public void UpdateChildClasses()
        {
            foreach (VisualElement child in previousChildren)
            {
                child.RemoveFromClassList(firstChildUssClassName);
                child.RemoveFromClassList(middleChildUssClassName);
                child.RemoveFromClassList(lastChildUssClassName);
                child.RemoveFromClassList(onlyChildUssClassName);
                child.RemoveFromClassList(oddChildUssClassName);
                child.RemoveFromClassList(evenChildUssClassName);
            }

            previousChildren.Clear();

            firstChild = null;
            lastChild = null;
            onlyChild = null;

            int childCount = hierarchy.childCount;
            if (childCount <= 0)
                return;

            firstChild = hierarchy[0];
            lastChild = hierarchy[childCount - 1];
            onlyChild = childCount == 1 ? firstChild : null;

            for (int i = 0; i < childCount; i++)
            {
                VisualElement child = hierarchy[i];
                previousChildren.Add(child);

                child.EnableInClassList(firstChildUssClassName, i == 0);
                child.EnableInClassList(middleChildUssClassName, i > 0 && i < childCount - 1);
                child.EnableInClassList(lastChildUssClassName, i == childCount - 1);
                child.EnableInClassList(onlyChildUssClassName, childCount == 1);

                // Match CSS :nth-child(odd/even): the first child is odd.
                child.EnableInClassList(oddChildUssClassName, (i & 1) == 0);
                child.EnableInClassList(evenChildUssClassName, (i & 1) != 0);
            }
        }
    }
}