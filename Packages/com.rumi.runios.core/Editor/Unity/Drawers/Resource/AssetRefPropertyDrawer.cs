#nullable enable
using RuniOS.Editor.IMGUI;
using RuniOS.Editor.Unity.Serialization;
using RuniOS.Resource;

namespace RuniOS.Editor.Unity.Drawers.Resource
{
    [CustomPropertyDrawer(typeof(IAssetRef), true)]
    public class AssetRefPropertyDrawer : PropertyDrawer
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
            if (converter?.Read(property, typeof(IAssetRef)) is not IAssetRef assetRef)
            {
                EditorGUI.LabelField(position, label, GUIContent.none);
                return;
            }

            RuniFields.AssetRefField(position, label, assetRef);
            converter.Write(property, typeof(ResourceKey), assetRef);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            PropertyConverter? converter = PropertyConverter.FindConverter(property);
            if (converter?.Read(property, typeof(IAssetRef)) is not IAssetRef assetRef)
                return EditorGUIUtility.singleLineHeight;

            return RuniFields.GetAssetRefFieldHeight(label, assetRef);
        }

        public static SerializedProperty GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();

            property.Next(true);
            return property;
        }
    }
}