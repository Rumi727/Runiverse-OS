#nullable enable

using RuniOS.Editor.UIElements;
using RuniOS.Editor.UIElements.IO;
using RuniOS.IO;
using UnityEngine.UIElements;

namespace RuniOS.Editor.IMGUI.Drawers.IO;

[CustomPropertyDrawer(typeof(FileExtension))]
public class FileExtensionPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property) => new FileExtensionField().SetProperty(property);

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        Draw(position, property, label);
        EditorGUI.EndProperty();
    }

    public static void Draw(Rect position, SerializedProperty property, GUIContent label)
    {
        property = GetChildProperty(property);
            
        EditorGUI.BeginChangeCheck();
        FileExtension value = FileExtensionField(position, label, property.stringValue); //boxedValue 쓰면 크래시남..
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