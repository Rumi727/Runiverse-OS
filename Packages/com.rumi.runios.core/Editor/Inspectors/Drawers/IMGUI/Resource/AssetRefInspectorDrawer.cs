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
            if (value.IsNull())
            {
                CheckVariableElement();
                EditorGUI.LabelField(position, label, new GUIContent(nullText ?? $"null ({variableElement.variableType.GetTypeDisplayName()})"));

                return value;
            }

            AssetRefField(position, label, (IAssetRef)value);
            return value;
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            CheckVariableElement();

            IAssetRef? value = (IAssetRef?)variableElement.GetValueOrDefault(flags);
            if (value.IsNull())
                return EditorGUIUtility.singleLineHeight;
            else
                return GetAssetRefFieldHeight(label, value);
        }

        protected override object CreateSnapshot(object? value) => ((IAssetRef)value!).key;

        protected override void ApplySnapshot(object? value, InspectorFlags flags)
        {
            CheckVariableElement();

            IAssetRef? currentValue = (IAssetRef?)variableElement.GetValueOrDefault(flags);
            if (currentValue != null)
                currentValue.key = (ResourceKey)value!;
        }
    }
}