#nullable enable
using RuniEngine.Editor.Drawers.IO;
using RuniEngine.Resource;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Drawers.Resource
{
    [CustomPropertyDrawer(typeof(PackIdentifier))]
    public class PackIdentifierPropertyDrawer : PropertyDrawer
    {
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
                
                PackIdentifierMode mode = internalIDToggle.boolValue ? PackIdentifierMode.id : PackIdentifierMode.path;
                EditorGUI.BeginChangeCheck();
                mode = (PackIdentifierMode)EditorGUI.EnumPopup(position, mode);
                if (EditorGUI.EndChangeCheck())
                {
                    internalIDToggle.boolValue = mode switch
                    {
                        PackIdentifierMode.id => true,
                        PackIdentifierMode.path => false,
                        _ => internalIDToggle.boolValue
                    };
                    
                    localPathToggle.boolValue = mode switch
                    {
                        PackIdentifierMode.id => false,
                        PackIdentifierMode.path => true,
                        _ => localPathToggle.boolValue
                    };
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (EditorGUIUtility.wideMode)
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

        public enum PackIdentifierMode
        {
            id,
            path
        }
    }
}
