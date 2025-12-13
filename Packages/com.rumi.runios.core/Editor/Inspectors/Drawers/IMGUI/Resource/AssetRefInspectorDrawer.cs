#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using RuniOS.Resource;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Resource
{
    [CustomInspectorDrawer(typeof(IAssetRef), true)]
    public class AssetRefInspectorDrawer : GenericInspectorDrawer
    {
        public AssetRefInspectorDrawer(IInspectorVariableElement element) : base(element) { }

        protected override object? DrawField(Rect position, GUIContent label, object? value, bool isInArray)
        {
            AssetRefField(position, label, (IAssetRef?)value ?? new AssetRef<object>());
            return value;
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            CheckVariableElement();

            bool isReadable = !variableElement.inspectable.instancesIsEmpty && variableElement.IsReadable(flags);
            IAssetRef value = (IAssetRef?)(isReadable ? variableElement.value : variableElement.variableType.GetDefaultValue()) ?? new AssetRef<object>();

            return GetAssetRefFieldHeight(label, value);
        }
    }
}