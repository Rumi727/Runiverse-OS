#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static void AssetRefField(IAssetRef value) => AssetRefField(GUIContent.none, value);
        public static void AssetRefField(string label, IAssetRef value) => AssetRefField(new GUIContent(label), value);
        public static void AssetRefField(GUIContent label, IAssetRef value) => RuniFields.AssetRefField(EditorGUILayout.GetControlRect(LabelHasContent(label), RuniFields.GetAssetRefFieldHeight(label, value)), label, value);
    }
}
