#nullable enable
using RuniOS.Editor.Drawers.IO;
using RuniOS.Editor.UIElements;
using RuniOS.Resource;
using RuniOS.UIElements.Resource;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Drawers.Resource
{
    [CustomPropertyDrawer(typeof(Identifier))]
    public class IdentifierPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new IdentifierField().SetProperty(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property.Copy());
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }

        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            (SerializedProperty nameSpace, SerializedProperty path) = GetChildProperty(property);
            path = FilePathPropertyDrawer.GetChildProperty(path);

            Identifier value = Identifier.empty;
            EditorGUI.BeginChangeCheck();

            string nameSpaceValue = nameSpace.stringValue;
            string pathValue = path.stringValue;
            
            if (Identifier.IsNamespaceValid(nameSpaceValue) && Identifier.IsPathValid(pathValue))
                value = new Identifier(nameSpace.stringValue, path.stringValue);
            
            value = IdentifierField(position, label, value);
            
            if (EditorGUI.EndChangeCheck())
            {
                nameSpace.stringValue = value.nameSpace;
                path.stringValue = value.path;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (EditorGUIUtility.wideMode)
                return EditorGUIUtility.singleLineHeight;
            else
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
        }
        
        public static (SerializedProperty nameSpace, SerializedProperty path) GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            
            property.Next(true);
            SerializedProperty nameSpace = property.Copy();

            property.Next(false);
            SerializedProperty path = property;

            return (nameSpace, path);
        }
    }
}
