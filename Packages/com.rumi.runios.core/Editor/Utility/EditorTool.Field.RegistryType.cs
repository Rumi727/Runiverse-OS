#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.IMGUI;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static RegistryType RegistryTypeFieldLayout(RegistryType value) => RegistryTypeField(EditorGUILayout.GetControlRect(), value);
        public static RegistryType RegistryTypeFieldLayout(string label, RegistryType value) => RegistryTypeField(EditorGUILayout.GetControlRect(), label, value);
        public static RegistryType RegistryTypeFieldLayout(GUIContent label, RegistryType value) => RegistryTypeField(EditorGUILayout.GetControlRect(), label, value);

        public static RegistryType RegistryTypeField(Rect position, RegistryType value) => DoRegistryTypeField(position, value);
        public static RegistryType RegistryTypeField(Rect position, string label, RegistryType value) => RegistryTypeField(position, new GUIContent(label), value);
        public static RegistryType RegistryTypeField(Rect position, GUIContent label, RegistryType value)
        {
            position = EditorGUI.PrefixLabel(position, label);
            BeginIndentLevel(0);
            value = DoRegistryTypeField(position, value);
            EndIndentLevel();
            return value;
        }

        static int? registryTypeFieldLastControlID;
        static string registryTypeFieldSelectedNamespace = string.Empty;
        static RegistryType DoRegistryTypeField(Rect position, RegistryType value)
        {
            position.height = EditorGUIUtility.singleLineHeight;
        
            TextDropdown dropdown = new TextDropdown();
            dropdown.Rebuild(ResourceManager.assetRegistries.Select(x => x.registryName));

            value = TextFieldDropDown(position, value, out bool isPressed);
            if (isPressed)
                dropdown.Show(position);

            int lastControlID = EditorGUIUtilityBridge.s_LastControlID;
            dropdown.onSelectedItem += x =>
            {
                registryTypeFieldLastControlID = lastControlID;
                registryTypeFieldSelectedNamespace = x.value;
            };

            if (registryTypeFieldLastControlID == lastControlID)
            {
                value = registryTypeFieldSelectedNamespace;

                registryTypeFieldSelectedNamespace = string.Empty;
                registryTypeFieldLastControlID = null;

                GUI.changed = true;
            }
        
            return value;
        }
    }
}