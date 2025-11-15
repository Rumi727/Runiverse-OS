#nullable enable
using RuniOS.Editor.Serialization;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI.Drawers.Resource;

[CustomPropertyDrawer(typeof(RegistryType))]
public class RegistryTypePropertyDrawer : PropertyDrawer
{
    //public override VisualElement CreatePropertyGUI(SerializedProperty property) => new PackIdentifierField().SetProperty(property);

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
        PropertyConverter? converter = PropertyConverter.FindConverter(property);
        if (converter?.Read(property, typeof(RegistryType)) is not RegistryType registryType)
        {
            EditorGUI.LabelField(position, label, GUIContent.none);
            return;
        }

        registryType = RegistryTypeField(position, label, registryType);
        converter.Write(property, typeof(RegistryType), registryType);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

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