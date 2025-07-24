#nullable enable
using RuniEngine.Editor.APIBridge.UnityEditorInternal;
using UnityEditor;

namespace RuniEngine.Editor
{
    public static partial class EditorStaticTool
    {
        public static bool IsChildrenIncluded(this SerializedProperty prop) => prop.propertyType == SerializedPropertyType.Generic;
        public static bool IsTextField(this SerializedProperty prop) => prop.propertyType == SerializedPropertyType.Integer || prop.propertyType == SerializedPropertyType.Float || prop.propertyType == SerializedPropertyType.Character || prop.propertyType == SerializedPropertyType.String;

        public static void SetDefaultValue(this SerializedProperty serializedProperty)
        {
            if (serializedProperty.isArray)
            {
                serializedProperty.ClearArray();
                return;
            }

            if (serializedProperty.propertyType == SerializedPropertyType.String)
            {
                serializedProperty.stringValue = string.Empty;
                return;
            }

            if (serializedProperty.boxedValue != null)
            {
                serializedProperty.boxedValue = serializedProperty.boxedValue.GetType().GetDefaultValue();
                return;
            }
        }

        public static bool IsNullable(this SerializedProperty serializedProperty) => serializedProperty.propertyType == SerializedPropertyType.ManagedReference || serializedProperty.propertyType == SerializedPropertyType.ObjectReference || serializedProperty.propertyType == SerializedPropertyType.ExposedReference || serializedProperty.propertyType == SerializedPropertyType.String;

        public static SerializedProperty? GetParent(this SerializedProperty serializedProperty)
        {
            string path = serializedProperty.propertyPath;
            if (path.Contains('.'))
            {
                int index = path.LastIndexOf('.');
                path = path.Substring(0, index);

                return serializedProperty.serializedObject.FindProperty(path);
            }

            return null;
        }

        public static bool IsInArray(this SerializedProperty? serializedProperty)
        {
            while ((serializedProperty = serializedProperty?.GetParent()) != null)
            {
                if (serializedProperty.isArray)
                    return true;
            }

            return false;
        }

        public static string GetIdentifier(this SerializedProperty serializedProperty) => ReorderableListWrapper.GetPropertyIdentifier(serializedProperty);
    }
}
