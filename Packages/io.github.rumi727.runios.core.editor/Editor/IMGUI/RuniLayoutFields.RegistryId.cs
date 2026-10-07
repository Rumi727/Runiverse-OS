#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static Identifier RegistryIdField(Identifier value, Func<IAssetRegistry, bool>? predicate = null) => RegistryIdField(GUIContent.none, value, predicate);
        public static Identifier RegistryIdField(string label, Identifier value, Func<IAssetRegistry, bool>? predicate = null) => RegistryIdField(new GUIContent(label), value, predicate);
        public static Identifier RegistryIdField(GUIContent label, Identifier value, Func<IAssetRegistry, bool>? predicate = null) => RuniFields.RegistryIdField(GetMultiColumnsControlRect(label), label, value, predicate);
    }
}
