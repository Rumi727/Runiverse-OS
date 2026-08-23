#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static AssetRef<T> AssetRefField<T>(AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(GUIContent.none, (IAssetRef)value, allowSceneObjects);
        public static AssetRef<T> AssetRefField<T>(string label, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(new GUIContent(label), (IAssetRef)value, allowSceneObjects);
        public static AssetRef<T> AssetRefField<T>(GUIContent label, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(new GUIContent(label), (IAssetRef)value, allowSceneObjects);

        public static IAssetRef AssetRefField(IAssetRef value, bool allowSceneObjects = false) => AssetRefField(GUIContent.none, value, allowSceneObjects);
        public static IAssetRef AssetRefField(string label, IAssetRef value, bool allowSceneObjects = false) => AssetRefField(new GUIContent(label), value, allowSceneObjects);
        public static IAssetRef AssetRefField(GUIContent label, IAssetRef value, bool allowSceneObjects = false) => RuniFields.AssetRefField(EditorGUILayout.GetControlRect(LabelHasContent(label), RuniFields.GetAssetRefFieldHeight(label, value)), label, value, allowSceneObjects);
    }
}
