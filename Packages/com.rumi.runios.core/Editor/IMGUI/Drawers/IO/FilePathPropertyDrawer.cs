#nullable enable
using RuniOS.Editor.UIElements;
using RuniOS.Editor.UIElements.IO;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.Editor.IMGUI.Drawers.IO
{
    [CustomPropertyDrawer(typeof(FilePath))]
    public class FilePathPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new FilePathField().SetProperty(property);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Draw(position, property, label);
            try
            {
                EditorGUI.EndProperty();
            }
            catch (InvalidOperationException)
            {
                
            }
        }
        
        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            property = GetChildProperty(property);
            
            EditorGUI.BeginChangeCheck();
            string value = FilePathField(position, label, property.stringValue);
            if (EditorGUI.EndChangeCheck())
                property.stringValue = value;
        }

        public static SerializedProperty GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();
            property.Next(true);

            return property;
        }
    }
}