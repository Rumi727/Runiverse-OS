#nullable enable
using RuniEngine.Editor.APIBridge.UnityEditorInternal;
using UnityEditor;

namespace RuniEngine.Editor
{
    public static partial class EditorStaticTool
    {
        public static bool IsGeneric(this SerializedProperty property) => property.propertyType == SerializedPropertyType.Generic;
        public static bool IsTextField(this SerializedProperty property) => property.propertyType == SerializedPropertyType.Integer || property.propertyType == SerializedPropertyType.Float || property.propertyType == SerializedPropertyType.Character || property.propertyType == SerializedPropertyType.String;

        public static void SetDefaultValue(this SerializedProperty property)
        {
            if (property.isArray)
            {
                property.ClearArray();
                return;
            }

            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = string.Empty;
                return;
            }

            if (property.boxedValue != null)
            {
                property.boxedValue = property.boxedValue.GetType().GetDefaultValue();
                return;
            }
        }

        public static bool IsNullable(this SerializedProperty property) => property.propertyType == SerializedPropertyType.ManagedReference || property.propertyType == SerializedPropertyType.ObjectReference || property.propertyType == SerializedPropertyType.ExposedReference || property.propertyType == SerializedPropertyType.String;

        public static SerializedProperty? GetParent(this SerializedProperty property)
        {
            string path = property.propertyPath;
            if (path.Contains('.'))
            {
                int index = path.LastIndexOf('.');
                path = path.Substring(0, index);

                return property.serializedObject.FindProperty(path);
            }

            return null;
        }

        public static bool IsInArray(this SerializedProperty? property)
        {
            while ((property = property?.GetParent()) != null)
            {
                if (property.isArray)
                    return true;
            }

            return false;
        }

        public static string GetGlobalIdentifier(this SerializedProperty property) => ReorderableListWrapper.GetPropertyIdentifier(property);
    }
}
