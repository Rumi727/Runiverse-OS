#nullable enable
using RuniOS.UIElements;
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements.Primitives
{
    [CustomInspectorDrawer(typeof(short))]
    public class ShortInspectorDrawer : UIElementInspectorDrawer
    {
        public ShortInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ShortInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public override VisualElement Build() => new ShortField();
    }
}