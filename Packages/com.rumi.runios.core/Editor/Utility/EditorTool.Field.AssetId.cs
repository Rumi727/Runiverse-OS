#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.IMGUI;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static Identifier AssetIdFieldLayout(Identifier registryId, Identifier value) => AssetIdFieldLayout(GUIContent.none, registryId, value);
        public static Identifier AssetIdFieldLayout(string label, Identifier registryId, Identifier value) => AssetIdFieldLayout(new GUIContent(label), registryId, value);
        public static Identifier AssetIdFieldLayout(GUIContent label, Identifier registryId, Identifier value) => AssetIdField(GetMultiColumnsControlRect(label), label, registryId, value);

        public static Identifier AssetIdField(Rect position, Identifier registryId, Identifier value) => DoAssetIdField(position, registryId, value);
        public static Identifier AssetIdField(Rect position, string label, Identifier registryId, Identifier value) => AssetIdField(position, new GUIContent(label), registryId, value);
        public static Identifier AssetIdField(Rect position, GUIContent label, Identifier registryId, Identifier value)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoAssetIdField(position, registryId, value);
        }

        static int? assetIdFieldLastControlID;
        static RuniPath assetIdFieldSelectedPath = string.Empty;
        static Identifier DoAssetIdField(Rect position, Identifier registryId, Identifier value)
        {
            string currentNamespace = value.nameSpace;
            RuniPathDropdown dropdown = new RuniPathDropdown();

            value = IdentifierField(position, value, x =>
            {
                IEnumerable<RuniPath>? assetPaths = AssetRegistryManager.Get(registryId)?.keys
                    .Where(x => currentNamespace == x.nameSpace)
                    .Select(x => x.path);

                dropdown.Rebuild(assetPaths ?? Enumerable.Empty<RuniPath>());
                dropdown.Show(x);
            });

            int lastControlID = EditorGUIUtilityBridge.s_LastControlID;
            dropdown.onSelectedItem += x =>
            {
                registryTypeFieldLastControlID = lastControlID;
                registryTypeFieldSelectedPath = x.path;
            };

            if (registryTypeFieldLastControlID == lastControlID)
            {
                value.path = registryTypeFieldSelectedPath;

                registryTypeFieldSelectedPath = string.Empty;
                registryTypeFieldLastControlID = null;

                GUI.changed = true;
            }

            return value;
        }
    }
}