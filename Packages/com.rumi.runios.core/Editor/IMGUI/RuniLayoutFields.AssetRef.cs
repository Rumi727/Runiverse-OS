#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static AssetRef<T> AssetRefField<T>(AssetRef<T> value) where T : notnull => (AssetRef<T>)AssetRefField(GUIContent.none, (IAssetRef)value);
        public static AssetRef<T> AssetRefField<T>(string label, AssetRef<T> value) where T : notnull => (AssetRef<T>)AssetRefField(new GUIContent(label), (IAssetRef)value);
        public static AssetRef<T> AssetRefField<T>(GUIContent label, AssetRef<T> value) where T : notnull => (AssetRef<T>)AssetRefField(new GUIContent(label), (IAssetRef)value);

        public static IAssetRef AssetRefField(IAssetRef value) => AssetRefField(GUIContent.none, value);
        public static IAssetRef AssetRefField(string label, IAssetRef value) => AssetRefField(new GUIContent(label), value);
        public static IAssetRef AssetRefField(GUIContent label, IAssetRef value) => RuniFields.AssetRefField(EditorGUILayout.GetControlRect(LabelHasContent(label), RuniFields.GetAssetRefFieldHeight(label, value)), label, value);
    }
}
