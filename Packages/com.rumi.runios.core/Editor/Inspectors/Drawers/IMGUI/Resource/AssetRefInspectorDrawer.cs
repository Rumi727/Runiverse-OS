#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Resource;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Resource
{
    [CustomInspectorDrawer(typeof(IAssetRef), true)]
    public class AssetRefInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : GenericInspectorDrawer(element, inheritedAttributes, undoRecorder)
    {
        protected override object? DrawField(Rect position, GUIContent label, object? value, DrawerContext context = default)
        {
            if (value.IsNull())
            {
                CheckVariableElement();
                EditorGUI.LabelField(position, label, new GUIContent(nullText ?? $"null ({variableElement.variableType.GetTypeDisplayName()})"));

                return value;
            }

            return RuniFields.AssetRefField(position, label, (IAssetRef)value);
        }

        protected override float CalculationHeight(GUIContent label, InspectorFlags flags, DrawerContext context = default)
        {
            CheckVariableElement();

            IAssetRef? value = (IAssetRef?)variableElement.GetValueOrDefault(flags);
            if (value.IsNull())
                return EditorGUIUtility.singleLineHeight;
            else
                return RuniFields.GetAssetRefFieldHeight(label, value);
        }
    }
}