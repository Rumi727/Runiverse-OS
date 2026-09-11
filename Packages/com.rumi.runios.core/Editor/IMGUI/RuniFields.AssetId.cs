#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static Identifier AssetIdField(Rect position, Identifier registryId, Identifier value) => DoAssetIdField(position, registryId, value);
        public static Identifier AssetIdField(Rect position, string label, Identifier registryId, Identifier value) => AssetIdField(position, new GUIContent(label), registryId, value);
        public static Identifier AssetIdField(Rect position, GUIContent label, Identifier registryId, Identifier value)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoAssetIdField(position, registryId, value);
        }

        public static Identifier AssetIdField(Rect position, Type assetType, Identifier value) => DoAssetIdField(position, assetType, value);
        public static Identifier AssetIdField(Rect position, string label, Type assetType, Identifier value) => AssetIdField(position, new GUIContent(label), assetType, value);
        public static Identifier AssetIdField(Rect position, GUIContent label, Type assetType, Identifier value)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoAssetIdField(position, assetType, value);
        }

        static Identifier DoAssetIdField(Rect position, Identifier registryId, Identifier value) => DoAssetIdField
        (
            position,
            value,
            AssetRegistryManager.Get(registryId)?.keys
                .Where(x => value.nameSpace == x.nameSpace)
                .Select(x => x.path) ?? []
        );

        static Identifier DoAssetIdField(Rect position, Type assetType, Identifier value) => DoAssetIdField
        (
            position,
            value,
            AssetRegistryManager.GetAllForAsset(assetType)
                .SelectMany(x => x.keys)
                .Where(x => value.nameSpace == x.nameSpace)
                .Select(x => x.path)
                .Distinct()
        );

        static int? assetIdFieldLastControlID;
        static RuniPath assetIdFieldSelectedPath = RuniPath.empty;
        static Identifier DoAssetIdField(Rect position, Identifier value, IEnumerable<RuniPath> assetPaths)
        {
            value = IdentifierField(position, value, x =>
            {
                int lastControlID = EditorGUIUtilityBridge.s_LastControlID;

                RuniPathDropdown dropdown = new RuniPathDropdown();
                dropdown.onSelectedItem += x =>
                {
                    assetIdFieldLastControlID = lastControlID;
                    assetIdFieldSelectedPath = x.path;
                };

                dropdown.Rebuild(assetPaths);
                dropdown.Show(x);
            });

            if (assetIdFieldLastControlID == EditorGUIUtilityBridge.s_LastControlID)
            {
                value.path = assetIdFieldSelectedPath;

                assetIdFieldSelectedPath = RuniPath.empty;
                assetIdFieldLastControlID = null;

                GUI.changed = true;
            }

            return value;
        }
    }
}
