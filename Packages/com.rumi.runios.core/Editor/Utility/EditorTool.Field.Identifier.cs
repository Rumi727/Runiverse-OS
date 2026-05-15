#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.IMGUI;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static Identifier IdentifierFieldLayout(Identifier value, Action<Rect>? dropdownAction = null) => IdentifierFieldLayout(GUIContent.none, value, dropdownAction);
        public static Identifier IdentifierFieldLayout(string label, Identifier value, Action<Rect>? dropdownAction = null) => IdentifierFieldLayout(new GUIContent(label), value, dropdownAction);
        public static Identifier IdentifierFieldLayout(GUIContent label, Identifier value, Action<Rect>? dropdownAction = null) => IdentifierField(GetMultiColumnsControlRect(label), label, value, dropdownAction);

        public static Identifier IdentifierField(Rect position, Identifier value, Action<Rect>? dropdownAction = null) => DoIdentifierField(position, value, dropdownAction);
        public static Identifier IdentifierField(Rect position, string label, Identifier value, Action<Rect>? dropdownAction = null) => IdentifierField(position, new GUIContent(label), value, dropdownAction);
        public static Identifier IdentifierField(Rect position, GUIContent label, Identifier value, Action<Rect>? dropdownAction = null)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoIdentifierField(position, value, dropdownAction);
        }

        static int? identifierFieldLastControlID;
        static string identifierFieldSelectedNamespace = string.Empty;
        static Identifier DoIdentifierField(Rect position, Identifier value, Action<Rect>? dropdownAction)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            BeginIndentLevel(0);
            float fieldWidth = (position.width - (2 * 4) - (4 * 2)) / 3f;

            {
                position.width = fieldWidth;

                TextDropdown nameSpaceDropdown = new TextDropdown();
                string nameSpace = TextFieldDropDown(position, value.nameSpace, out bool isPressed);
                if (isPressed)
                {
                    nameSpaceDropdown.Rebuild(ResourcePack.loadedResourcePacks.SelectMany(x => x.Value.namespaces));
                    nameSpaceDropdown.Show(position);
                }

                int lastControlID = EditorGUIUtilityBridge.s_LastControlID;
                nameSpaceDropdown.onSelectedItem += x =>
                {
                    identifierFieldLastControlID = lastControlID;
                    identifierFieldSelectedNamespace = x.value;
                };

                if (identifierFieldLastControlID == lastControlID)
                {
                    nameSpace = identifierFieldSelectedNamespace;

                    identifierFieldSelectedNamespace = string.Empty;
                    identifierFieldLastControlID = null;

                    GUI.changed = true;
                }

                if (Identifier.IsNamespaceValid(nameSpace))
                    value.nameSpace = nameSpace;
                else
                    Debug.LogWarning(Identifier.GetInvalidNamespaceMessage(nameSpace));

                position.x += position.width + 4;
            }

            {
                position.width = 8;
                position.x -= 4;

                GUI.Label(position, Identifier.separator.ToString());

                position.x += position.width;
            }

            {
                position.width = (fieldWidth * 2) + 8;

                RuniPath path;
                if (dropdownAction != null)
                {
                    path = (RuniPath)TextFieldDropDown(position, value.path.value, out bool isPressed);
                    if (isPressed)
                        dropdownAction.Invoke(position);
                }
                else
                    path = (RuniPath)EditorGUI.TextField(position, value.path.value);

                if (Identifier.IsPathValid(path))
                    value.path = path;
                else
                    Debug.LogWarning(Identifier.GetInvalidPathMessage(path));
            }

            EndIndentLevel();
            return value;
        }
    }
}