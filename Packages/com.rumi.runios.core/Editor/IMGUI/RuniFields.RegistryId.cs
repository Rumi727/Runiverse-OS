#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static Identifier RegistryIdField(Rect position, Identifier value, Func<IAssetRegistry, bool>? predicate = null) => DoRegistryIdField(position, value, predicate);
        public static Identifier RegistryIdField(Rect position, string label, Identifier value, Func<IAssetRegistry, bool>? predicate = null) => RegistryIdField(position, new GUIContent(label), value, predicate);
        public static Identifier RegistryIdField(Rect position, GUIContent label, Identifier value, Func<IAssetRegistry, bool>? predicate = null)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoRegistryIdField(position, value, predicate);
        }

        static int? registryIdFieldLastControlID;
        static string registryIdFieldSelectedNamespace = string.Empty;
        static Identifier DoRegistryIdField(Rect position, Identifier value, Func<IAssetRegistry, bool>? predicate)
        {
            string currentNamespace = value.nameSpace;
            TextDropdown dropdown = new TextDropdown();

            value = IdentifierField(position, value, x =>
            {
                IEnumerable<IAssetRegistry> registryIds = AssetRegistryManager.GetAll();
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
                registryTypeFieldSelectedPath = (RuniPath)x.value;
            };

            if (registryTypeFieldLastControlID == lastControlID)
            {
                value.path = registryTypeFieldSelectedPath;

                registryTypeFieldSelectedPath = RuniPath.empty;
                registryTypeFieldLastControlID = null;

                GUI.changed = true;
            }

            return value;
        }
    }
}
