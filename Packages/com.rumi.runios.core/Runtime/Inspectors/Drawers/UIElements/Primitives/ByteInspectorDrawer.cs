#nullable enable
using RuniOS.UIElements;
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements.Primitives
{
    [CustomInspectorDrawer(typeof(byte))]
    public class ByteInspectorDrawer : UIElementInspectorDrawer
    {
        public ByteInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ByteInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public override VisualElement Build() => new ByteField();
    }
}