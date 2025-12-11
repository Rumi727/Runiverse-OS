#nullable enable
using RuniOS.Editor.Unity.Serialization;
using RuniOS.Resource;

namespace RuniOS.Editor.Unity.Drawers.Resource
{
    [CustomPropertyDrawer(typeof(PackIdentifier))]
    public class PackIdentifierPropertyDrawer : PropertyDrawer
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
            if (converter?.Read(property, typeof(PackIdentifier)) is not PackIdentifier packIdentifier)
            {
                EditorGUI.LabelField(position, label, GUIContent.none);
                return;
            }

            packIdentifier = PackIdentifierField(position, label, packIdentifier);
            converter.Write(property, typeof(PackIdentifier), packIdentifier);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => GetMultiColumnsFieldHeight(label);

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