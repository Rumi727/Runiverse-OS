#nullable enable
using RuniOS.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements.Primitives
{
    [CustomInspectorDrawer(typeof(int))]
    public class IntegerInspectorDrawer : UIElementInspectorDrawer
    {
        public IntegerInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public IntegerInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public override VisualElement Build() => new IntegerField();
    }
}