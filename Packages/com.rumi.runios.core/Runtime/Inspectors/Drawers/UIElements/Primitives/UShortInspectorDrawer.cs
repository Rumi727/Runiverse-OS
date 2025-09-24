#nullable enable
using RuniOS.UIElements;
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements.Primitives
{
    [CustomInspectorDrawer(typeof(ushort))]
    public class UShortInspectorDrawer : UIElementInspectorDrawer
    {
        public UShortInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public UShortInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public override VisualElement Build() => new UShortField();
    }
}