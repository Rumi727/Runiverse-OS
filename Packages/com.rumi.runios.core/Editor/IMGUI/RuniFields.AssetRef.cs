#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static AssetRef<T> AssetRefField<T>(Rect position, AssetRef<T> value) => (AssetRef<T>)AssetRefField(position, GUIContent.none, (IAssetRef)value);
        public static AssetRef<T> AssetRefField<T>(Rect position, string label, AssetRef<T> value) => (AssetRef<T>)AssetRefField(position, new GUIContent(label), (IAssetRef)value);
        public static AssetRef<T> AssetRefField<T>(Rect position, GUIContent label, AssetRef<T> value) => (AssetRef<T>)AssetRefField(position, new GUIContent(label), (IAssetRef)value);

        public static IAssetRef AssetRefField(Rect position, IAssetRef value) => AssetRefField(position, GUIContent.none, value);
        public static IAssetRef AssetRefField(Rect position, string label, IAssetRef value) => AssetRefField(position, new GUIContent(label), value);
        public static IAssetRef AssetRefField(Rect position, GUIContent label, IAssetRef value)
        {
            ReadOnlySet<IAssetRegistry> registries = AssetRegistryManager.GetAllForAsset(value.targetAssetType);
            IAssetRegistry? defaultRegistry = AssetRegistryManager.GetDefaultForAsset(value.targetAssetType);

            if (registries.Count > 1 || defaultRegistry == null)
                return value.WithKey(ResourceKeyField(position, label, value.key, registries.Contains));
            else
                return value.WithKey(new ResourceKey(defaultRegistry.registryId, AssetIdField(position, label, defaultRegistry.registryId, value.key.assetId)));
        }

        public static float GetAssetRefFieldHeight(GUIContent? label, IAssetRef value)
        {
            ReadOnlySet<IAssetRegistry> registries = AssetRegistryManager.GetAllForAsset(value.targetAssetType);
            IAssetRegistry? defaultRegistry = AssetRegistryManager.GetDefaultForAsset(value.targetAssetType);

            float height;
            if (registries.Count > 1 || defaultRegistry == null)
                height = GetMultiRowsFieldHeight(label, 2);
            else
                height = GetMultiColumnsFieldHeight(label);

            return height;
        }
    }
}
