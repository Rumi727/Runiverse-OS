#nullable enable
using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using System.Reflection;

namespace RuniOS.Editor.Inspectors.Attributes.IMGUI
{
    public abstract class IMGUIInspectorAttributeDrawer : InspectorAttributeDrawer
    {
        static readonly object?[] args = new object?[1];
        public static IMGUIInspectorAttributeDrawer? FindDrawer(IInspectorAttribute? attribute, Func<(Type type, CustomInspectorDrawerAttribute attribute), bool>? predicate = null)
        {
            if (attribute == null)
                return null;

            Type? type = AttributeTypeResolver<IMGUIInspectorAttributeDrawer, CustomInspectorDrawerAttribute>.FindDrawerType(attribute.GetType(), predicate);
            if (type == null)
                return null;

            args[0] = attribute;
            return (IMGUIInspectorAttributeDrawer)Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, args, null);
        }

        protected IMGUIInspectorAttributeDrawer(IInspectorAttribute attribute) : base(attribute) { }

        public abstract void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null);

        public virtual float GetHeight(IMGUIInspectorDrawer drawer, GUIContent? label, InspectorFlags flags, bool isInArray = false) => drawer.GetHeight(label, flags, isInArray);
    }
}