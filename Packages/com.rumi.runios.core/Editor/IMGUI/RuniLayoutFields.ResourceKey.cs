#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static ResourceKey ResourceKeyField(ResourceKey value, Func<IAssetRegistry, bool>? predicate = null) => ResourceKeyField(GUIContent.none, value, predicate);
        public static ResourceKey ResourceKeyField(string label, ResourceKey value, Func<IAssetRegistry, bool>? predicate = null) => ResourceKeyField(new GUIContent(label), value, predicate);
        public static ResourceKey ResourceKeyField(GUIContent label, ResourceKey value, Func<IAssetRegistry, bool>? predicate = null) => RuniFields.ResourceKeyField(GetMultiRowsControlRect(label, 2), label, value, predicate);
    }
}
