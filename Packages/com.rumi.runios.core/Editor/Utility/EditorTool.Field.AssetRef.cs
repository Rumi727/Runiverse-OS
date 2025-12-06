#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static void AssetRefFieldLayout(IAssetRef value) => AssetRefFieldLayout(GUIContent.none, value);
        public static void AssetRefFieldLayout(string label, IAssetRef value) => AssetRefFieldLayout(new GUIContent(label), value);
        public static void AssetRefFieldLayout(GUIContent label, IAssetRef value)
        {
            ReadOnlySet<AssetRegistry> registries = AssetRegistryManager.GetAllForAsset(value.targetAssetType);
            float height;
            if (registries.Count > 1)
                height = GetMultiRowsFieldHeight(label, 2);
            else
                height = GetMultiColumnsFieldHeight(label);

            AssetRefField(EditorGUILayout.GetControlRect(LabelHasContent(label), height), label, value);
        }

        public static void AssetRefField(Rect position, IAssetRef value) => AssetRefField(position, GUIContent.none, value);
        public static void AssetRefField(Rect position, string label, IAssetRef value) => AssetRefField(position, new GUIContent(label), value);
        public static void AssetRefField(Rect position, GUIContent label, IAssetRef value)
        {
            ReadOnlySet<AssetRegistry> registries = AssetRegistryManager.GetAllForAsset(value.targetAssetType);
            AssetRegistry? defaultRegistry = AssetRegistryManager.GetDefaultForAsset(value.targetAssetType);
            
            if (registries.Count > 1 || defaultRegistry == null)
                value.key = ResourceKeyField(position, label, value.key, x => registries.Contains(x));
            else
                value.key = new ResourceKey(defaultRegistry.registryId, AssetIdField(position, label, value.key.registryId, value.key.assetId));
        }
    }
}