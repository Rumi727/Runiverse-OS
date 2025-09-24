#nullable enable
using RuniOS.UIElements;
using RuniOS.UIElements.Primitives;
using UnityEngine.UIElements;

namespace RuniOS.Inspectors.Drawers.UIElements.Primitives
{
    [CustomInspectorDrawer(typeof(nint))]
    public class NativeIntegerInspectorDrawer : UIElementInspectorDrawer
    {
        public NativeIntegerInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public NativeIntegerInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }
        
        public override VisualElement Build() => new NativeIntegerField();
    }
}