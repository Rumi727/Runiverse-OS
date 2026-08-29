#nullable enable
using RuniOS.Editor.Inspectors.Drawers.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Reflection;
using System.Reflection;

namespace RuniOS.Editor.Inspectors.Attributes.IMGUI
{
    public abstract partial class IMGUIInspectorAttributeDrawer(IInspectorAttribute attribute) : InspectorAttributeDrawer(attribute)
    {
        [GenerateTypeRegistry]
        public static partial AttributedTypeRegistry<InspectorDrawerAttribute> registry { get; }

        static readonly object?[] args = new object?[1];
        public static IMGUIInspectorAttributeDrawer? FindDrawer(IInspectorAttribute? attribute, Func<RegistrationEntry<InspectorDrawerAttribute>, bool>? predicate = null)
        {
            if (attribute == null)
                return null;

            Type? type = registry.Resolve(attribute.GetType(), predicate);
            if (type == null)
                return null;

            args[0] = attribute;
            return (IMGUIInspectorAttributeDrawer)Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, args, null);
        }

        public void Draw(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default)
            => OnGUI(drawer, position, label, flags, context);

        protected abstract void OnGUI(IMGUIInspectorDrawer drawer, Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default);

        public virtual float GetHeight(IMGUIInspectorDrawer drawer, GUIContent? label, InspectorFlags flags, DrawerContext context = default) => drawer.GetHeight(label, flags, context);
    }
}