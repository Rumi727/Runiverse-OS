#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Resource;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Resource
{
    [CustomInspectorDrawer(typeof(IAssetRef), true)]
    public class AssetRefInspectorDrawer : GenericInspectorDrawer
    {
        public AssetRefInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }

        protected override object? DrawField(Rect position, GUIContent label, object? value, bool isInArray, Rect? clipping)
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

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, bool isInArray, Rect? clipping)
        {
            CheckVariableElement();

            IAssetRef? value = (IAssetRef?)variableElement.GetValueOrDefault(flags);
            if (value.IsNull())
                return EditorGUIUtility.singleLineHeight;
            else
                return GetAssetRefFieldHeight(label, value);
        }

        protected override object CreateSnapshot(object? value) => ((IAssetRef)value!).key;

        protected override void ApplySnapshot(IInspectorVariableElement variableElement, object? value, InspectorFlags flags)
        {
            IAssetRef? currentValue = (IAssetRef?)variableElement.GetValueOrDefault(flags);
            if (currentValue != null)
                currentValue.key = (ResourceKey)value!;
        }
    }
}