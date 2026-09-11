#nullable enable
using RuniOS.Collections.Generic;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static AssetRef<T> AssetRefField<T>(Rect position, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(position, GUIContent.none, (IAssetRef)value, allowSceneObjects);
        public static AssetRef<T> AssetRefField<T>(Rect position, string label, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(position, new GUIContent(label), (IAssetRef)value, allowSceneObjects);
        public static AssetRef<T> AssetRefField<T>(Rect position, GUIContent label, AssetRef<T> value, bool allowSceneObjects = false) where T : notnull => (AssetRef<T>)AssetRefField(position, label, (IAssetRef)value, allowSceneObjects);

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

            switch (value.mode)
            {
                case AssetRefMode.automatic:
                {
                    EditorGUI.BeginChangeCheck();

                    Identifier assetId = AssetIdField(position, label, value.targetAssetType, value.assetId);

                    if (EditorGUI.EndChangeCheck())
                        value = value.WithAssetId(assetId);
                    break;
                }
                case AssetRefMode.registry:
                {
                    ReadOnlySet<IAssetRegistry> registries = AssetRegistryManager.GetAllForAsset(value.targetAssetType);

                    EditorGUI.BeginChangeCheck();
                    ResourceKey resourceKey = ResourceKeyField(position, label, value.resourceKey, registries.Contains);

                    if (EditorGUI.EndChangeCheck())
                        value = value.WithResourceKey(resourceKey);
                    break;
                }
                case AssetRefMode.direct when isUnityObject:
                {
                    EditorGUI.BeginChangeCheck();
                    object? directAsset = EditorGUI.ObjectField(position, label, (Object?)value.directAsset, value.targetAssetType, allowSceneObjects);
                    if (EditorGUI.EndChangeCheck())
                        value = value.WithDirect(directAsset);
                    break;
                }
                case AssetRefMode.direct:
                    EditorGUI.LabelField(position, label, TempContent(value.directAsset?.ToString() ?? $"null ({value.targetAssetType.GetTypeDisplayName()})"));
                    break;
            }

            return value;
        }

        public static float GetAssetRefFieldHeight(GUIContent? label, IAssetRef value) => value.mode == AssetRefMode.registry
            ? GetMultiRowsFieldHeight(label, 2)
            : GetMultiColumnsFieldHeight(label);
    }
}
