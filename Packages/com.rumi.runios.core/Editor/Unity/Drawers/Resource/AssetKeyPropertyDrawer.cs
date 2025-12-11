#nullable enable
using RuniOS.Editor.Unity.Serialization;
using RuniOS.Resource;

namespace RuniOS.Editor.Unity.Drawers.Resource
{
    [CustomPropertyDrawer(typeof(ResourceKey))]
    public class AssetKeyPropertyDrawer : PropertyDrawer
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
            if (converter?.Read(property, typeof(ResourceKey)) is not ResourceKey registryType)
            {
                EditorGUI.LabelField(position, label, GUIContent.none);
                return;
            }

            registryType = ResourceKeyField(position, label, registryType);
            converter.Write(property, typeof(ResourceKey), registryType);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => GetMultiRowsFieldHeight(label, 2);

        public static (SerializedProperty registryId, SerializedProperty assetId) GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();

            property.Next(true);
            SerializedProperty registryId = property.Copy();

            property.Next(false);
            SerializedProperty assetId = property;

            return (registryId, assetId);
        }
    }
}