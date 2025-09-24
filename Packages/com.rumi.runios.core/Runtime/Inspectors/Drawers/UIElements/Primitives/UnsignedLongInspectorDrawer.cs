#nullable enable
using RuniOS.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements.Primitives
{
    [CustomInspectorDrawer(typeof(ulong))]
    public class UnsignedLongInspectorDrawer : UIElementInspectorDrawer
    {
        public UnsignedLongInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public UnsignedLongInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public override VisualElement Build() => new UnsignedLongField();
    }
}