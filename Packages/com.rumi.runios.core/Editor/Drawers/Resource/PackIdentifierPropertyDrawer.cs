#nullable enable
using RuniOS.Editor.Drawers.IO;
using RuniOS.Editor.UIElements;
using RuniOS.Resource;
using RuniOS.UIElements.Resource;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Drawers.Resource
{
    [CustomPropertyDrawer(typeof(PackIdentifier))]
    public class PackIdentifierPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new PackIdentifierField().SetProperty(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label);
            EditorGUI.EndProperty();
        }
        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            (SerializedProperty nullableInternalID, SerializedProperty nullableLocalPath) = GetChildProperty(property);
            (SerializedProperty? internalIDField, SerializedProperty? internalIDToggle) = SerializableNullablePropertyDrawer.GetChildProperty(nullableInternalID);
            (SerializedProperty? localPathField, SerializedProperty? localPathToggle) = SerializableNullablePropertyDrawer.GetChildProperty(nullableLocalPath);

            if (internalIDField != null && internalIDToggle != null && localPathField != null && localPathToggle != null)
            {
                position.width -= 54;
                if (internalIDToggle.boolValue)
                    IdentifierPropertyDrawer.Draw(position, internalIDField, label);
                else
                    FilePathPropertyDrawer.Draw(position, localPathField, label);
                
                if (!EditorGUIUtility.wideMode)
                    position.y += EditorGUIUtility.singleLineHeight + 2;

                position.x += position.width + 4;
                position.width = 50;
                
                PackIdentifierField.PackIdentifierMode mode = internalIDToggle.boolValue ? PackIdentifierField.PackIdentifierMode.id : PackIdentifierField.PackIdentifierMode.path;
                EditorGUI.BeginChangeCheck();
                mode = (PackIdentifierField.PackIdentifierMode)EditorGUI.EnumPopup(position, mode);
                if (EditorGUI.EndChangeCheck())
                {
                    internalIDToggle.boolValue = mode switch
                    {
                        PackIdentifierField.PackIdentifierMode.id => true,
                        PackIdentifierField.PackIdentifierMode.path => false,
                        _ => internalIDToggle.boolValue
                    };
                    
                    localPathToggle.boolValue = mode switch
                    {
                        PackIdentifierField.PackIdentifierMode.id => false,
                        PackIdentifierField.PackIdentifierMode.path => true,
                        _ => localPathToggle.boolValue
                    };
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (EditorGUIUtility.wideMode || !EditorTool.LabelHasContent(label))
                return EditorGUIUtility.singleLineHeight;
            else
                return (EditorGUIUtility.singleLineHeight * 2) + 2;
        }

        public static (SerializedProperty nullableInternalID, SerializedProperty nullableLocalPath) GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            
            property.Next(true);
            SerializedProperty internalID = property.Copy();

            property.Next(false);
            SerializedProperty localPath = property;

            return (internalID, localPath);
        }
    }
}
