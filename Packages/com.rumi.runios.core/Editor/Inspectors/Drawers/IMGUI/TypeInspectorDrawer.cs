#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    [CustomInspectorDrawer(typeof(Type))]
    public class TypeInspectorDrawer : GenericInspectorDrawer
    {
        public TypeInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object? DrawField(Rect position, GUIContent label, object? value, bool isInArray) => TypeField(position, label, (Type?)value);
    }
}