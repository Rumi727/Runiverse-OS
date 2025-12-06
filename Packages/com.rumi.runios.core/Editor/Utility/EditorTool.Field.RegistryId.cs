#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.IMGUI;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static Identifier RegistryIdFieldLayout(Identifier value, Func<AssetRegistry, bool>? predicate = null) => RegistryIdFieldLayout(GUIContent.none, value, predicate);
        public static Identifier RegistryIdFieldLayout(string label, Identifier value, Func<AssetRegistry, bool>? predicate = null) => RegistryIdFieldLayout(new GUIContent(label), value, predicate);
        public static Identifier RegistryIdFieldLayout(GUIContent label, Identifier value, Func<AssetRegistry, bool>? predicate = null) => RegistryIdField(GetMultiColumnsControlRect(label), label, value, predicate);

        public static Identifier RegistryIdField(Rect position, Identifier value, Func<AssetRegistry, bool>? predicate = null) => DoRegistryIdField(position, value, predicate);
        public static Identifier RegistryIdField(Rect position, string label, Identifier value, Func<AssetRegistry, bool>? predicate = null) => RegistryIdField(position, new GUIContent(label), value, predicate);
        public static Identifier RegistryIdField(Rect position, GUIContent label, Identifier value, Func<AssetRegistry, bool>? predicate = null)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoRegistryIdField(position, value, predicate);
        }

        static int? registryIdFieldLastControlID;
        static string registryIdFieldSelectedNamespace = string.Empty;
        static Identifier DoRegistryIdField(Rect position, Identifier value, Func<AssetRegistry, bool>? predicate)
        {
            string currentNamespace = value.nameSpace;
            TextDropdown dropdown = new TextDropdown();

            value = IdentifierField(position, value, x =>
            {
                IEnumerable<AssetRegistry> registryIds = AssetRegistryManager.GetAll();
                if (predicate != null)
                    registryIds = registryIds.Where(predicate);

                dropdown.Rebuild(registryIds
                    .Where(x => x.registryId.nameSpace == currentNamespace)
                    .Select(x => x.registryId.path.value)
                );

                dropdown.Show(x);
            });

            int lastControlID = EditorGUIUtilityBridge.s_LastControlID;
            dropdown.onSelectedItem += x =>
            {
                registryTypeFieldLastControlID = lastControlID;
                registryTypeFieldSelectedPath = x.value;
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