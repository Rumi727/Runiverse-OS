#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity.Editor
{
    [CustomPropertyDrawer(typeof(PackageReferenceAttribute))]
    sealed class PackageReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.LabelField(position, label, new GUIContent("Expected a package asset reference."));
                return;
            }
            EditorGUI.BeginProperty(position, label, property);
            Object value = EditorGUI.ObjectField(position, label, property.objectReferenceValue, typeof(ScriptableObject), false);
            if (value == null || value is IPackage) property.objectReferenceValue = value;
            else if (value != property.objectReferenceValue) Debug.LogWarning("Select a package definition implementing IPackage.", value);
            EditorGUI.EndProperty();
        }
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            => EditorGUIUtility.singleLineHeight;
    }
}
