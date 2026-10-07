#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static ResourceKey ResourceKeyField(Rect position, ResourceKey value, Func<IAssetRegistry, bool>? predicate = null) => DoResourceKeyField(position, value, false, predicate);
        public static ResourceKey ResourceKeyField(Rect position, string label, ResourceKey value, Func<IAssetRegistry, bool>? predicate = null) => ResourceKeyField(position, new GUIContent(label), value, predicate);
        public static ResourceKey ResourceKeyField(Rect position, GUIContent label, ResourceKey value, Func<IAssetRegistry, bool>? predicate = null)
        {
            position = DrawMultiRowsPrefixLabel(position, label);
            return DoResourceKeyField(position, value, LabelHasContent(label), predicate);
        }

        static int? registryTypeFieldLastControlID;
        static RuniPath registryTypeFieldSelectedPath = RuniPath.empty;
        static ResourceKey DoResourceKeyField(Rect position, ResourceKey value, bool isBelowLabel, Func<IAssetRegistry, bool>? predicate)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            position = DrawMultiRowsLabel(position, EditorGUIUtility.wideMode & isBelowLabel, new GUIContent(GetTextOrKey("gui.registry")), new GUIContent(GetTextOrKey("gui.asset")));

            value.registryId = RegistryIdField(position, value.registryId, predicate);
            position.y += EditorGUIUtility.singleLineHeight + EditorGUIBridge.kControlVerticalSpacingLegacy;
            
            value.assetId = AssetIdField(position, value.registryId, value.assetId);
            return value;
        }
    }
}
