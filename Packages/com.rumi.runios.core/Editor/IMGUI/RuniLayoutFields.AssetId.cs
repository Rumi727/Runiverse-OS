#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static Identifier AssetIdField(Identifier registryId, Identifier value) => AssetIdField(GUIContent.none, registryId, value);
        public static Identifier AssetIdField(string label, Identifier registryId, Identifier value) => AssetIdField(new GUIContent(label), registryId, value);
        public static Identifier AssetIdField(GUIContent label, Identifier registryId, Identifier value) => RuniFields.AssetIdField(GetMultiColumnsControlRect(label), label, registryId, value);
    }
}