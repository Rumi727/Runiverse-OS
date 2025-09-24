#nullable enable
using RuniOS.UIElements;
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements.Primitives
{
    [CustomInspectorDrawer(typeof(decimal))]
    public class DecimalInspectorDrawer : UIElementInspectorDrawer
    {
        public DecimalInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public DecimalInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public override VisualElement Build() => new DecimalField();
    }
}