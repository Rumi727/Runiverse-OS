#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static AssetRef<T> AssetRefField<T>(Rect position, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(position, GUIContent.none, (IAssetRef)value, allowSceneObjects);
        public static AssetRef<T> AssetRefField<T>(Rect position, string label, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(position, new GUIContent(label), (IAssetRef)value, allowSceneObjects);
        public static AssetRef<T> AssetRefField<T>(Rect position, GUIContent label, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(position, new GUIContent(label), (IAssetRef)value, allowSceneObjects);

        public static IAssetRef AssetRefField(Rect position, IAssetRef value, bool allowSceneObjects = false) => AssetRefField(position, GUIContent.none, value, allowSceneObjects);
        public static IAssetRef AssetRefField(Rect position, string label, IAssetRef value, bool allowSceneObjects = false) => AssetRefField(position, new GUIContent(label), value, allowSceneObjects);
        public static IAssetRef AssetRefField(Rect position, GUIContent label, IAssetRef value, bool allowSceneObjects = false)
        {
            bool isUnityObject = typeof(Object).IsAssignableFrom(value.targetAssetType);
            position.width -= 54;

            {
                Rect enumPosition = position;
                if (!EditorGUIUtility.wideMode)
                    enumPosition.y += EditorGUIUtility.singleLineHeight + 2;

                enumPosition.x += position.width + 4;
                enumPosition.width = 50;
                enumPosition.height = EditorGUIUtility.singleLineHeight;

                value = value.WithMode((AssetRefMode)EditorGUI.EnumPopup(enumPosition, value.mode));
            }

            if (value.mode == AssetRefMode.direct)
            {
                if (isUnityObject)
                {
                    EditorGUI.BeginChangeCheck();
                    object? directAsset = EditorGUI.ObjectField(position, label, (Object?)value.directAsset, value.targetAssetType, allowSceneObjects);
                    if (EditorGUI.EndChangeCheck())
                        value = value.WithDirect(directAsset);
                }
                else
                    EditorGUI.LabelField(position, label, TempContent(value.directAsset?.ToString() ?? $"null ({value.targetAssetType.GetTypeDisplayName()})"));
            }
            else
            {
                ReadOnlySet<IAssetRegistry> registries = AssetRegistryManager.GetAllForAsset(value.targetAssetType);
                IAssetRegistry? firstRegistry = AssetRegistryManager.GetFirstForAsset(value.targetAssetType);

                EditorGUI.BeginChangeCheck();

                ResourceKey key;
                if (registries.Count > 1 || firstRegistry == null)
                    key = ResourceKeyField(position, label, value.key, registries.Contains);
                else
                    key = new ResourceKey(firstRegistry.registryId, AssetIdField(position, label, firstRegistry.registryId, value.key.assetId));

                if (EditorGUI.EndChangeCheck())
                    value = value.WithKey(key);
            }

            return value;
        }

        public static float GetAssetRefFieldHeight(GUIContent? label, IAssetRef value)
        {
            if (value.mode == AssetRefMode.direct)
            {
                ReadOnlySet<IAssetRegistry> registries = AssetRegistryManager.GetAllForAsset(value.targetAssetType);
                IAssetRegistry? firstRegistry = AssetRegistryManager.GetFirstForAsset(value.targetAssetType);

                float height;
                if (registries.Count > 1 || firstRegistry == null)
                    height = GetMultiRowsFieldHeight(label, 2);
                else
                    height = GetMultiColumnsFieldHeight(label);

                return height;
            }
            else
                return GetMultiColumnsFieldHeight(label);
        }
    }
}
