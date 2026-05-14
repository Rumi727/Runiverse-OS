#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.IMGUI;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static PackIdentifier PackIdentifierFieldLayout(PackIdentifier value) => PackIdentifierFieldLayout(GUIContent.none, value);
        public static PackIdentifier PackIdentifierFieldLayout(string label, PackIdentifier value) => PackIdentifierFieldLayout(new GUIContent(label), value);
        public static PackIdentifier PackIdentifierFieldLayout(GUIContent label, PackIdentifier value) => PackIdentifierField(GetMultiColumnsControlRect(label), label, value);

        public static PackIdentifier PackIdentifierField(Rect position, PackIdentifier value) => DoPackIdentifierField(position, value);
        public static PackIdentifier PackIdentifierField(Rect position, string label, PackIdentifier value) => PackIdentifierField(position, new GUIContent(label), value);
        public static PackIdentifier PackIdentifierField(Rect position, GUIContent label, PackIdentifier value)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoPackIdentifierField(position, value);
        }

        static int? packIdentifierFieldLastControlID;
        static string packIdentifierFieldSelectedValue = string.Empty;
        static PackIdentifier DoPackIdentifierField(Rect position, PackIdentifier value)
        {
            position.width -= 54;
            if (value.path != null)
                value.path = RuniPathField(position, value.path.Value, true);
            else
            {
                TextDropdown valueDropdown = new TextDropdown();
                
                value.identifier ??= Identifier.empty;
                value.identifier = IdentifierField(position, value.identifier.Value, x =>
                {
                    valueDropdown.Rebuild
                    (
                        ResourcePack.loadedResourcePacks.Keys
                            .Where(x => x.identifier != null && x.identifier.Value.nameSpace == value.identifier.Value.nameSpace)
                            .Select(x => x.identifier!.Value.path.ToString())
                    );
                    valueDropdown.Show(x);
                });

                int lastControlID = EditorGUIUtilityBridge.s_LastControlID;
                valueDropdown.onSelectedItem += x =>
                {
                    packIdentifierFieldLastControlID = lastControlID;
                    packIdentifierFieldSelectedValue = x.value;
                };

                if (packIdentifierFieldLastControlID == lastControlID)
                {
                    value.identifier = new Identifier(value.identifier.Value.nameSpace, packIdentifierFieldSelectedValue);

                    packIdentifierFieldSelectedValue = string.Empty;
                    packIdentifierFieldLastControlID = null;

                    GUI.changed = true;
                }
            }

            if (!EditorGUIUtility.wideMode)
                position.y += EditorGUIUtility.singleLineHeight + 2;

            position.x += position.width + 4;
            position.width = 50;
            position.height = EditorGUIUtility.singleLineHeight;

            UIElements.Resource.PackIdentifierField.PackIdentifierMode mode = value.identifier != null ? UIElements.Resource.PackIdentifierField.PackIdentifierMode.id : UIElements.Resource.PackIdentifierField.PackIdentifierMode.path;
            EditorGUI.BeginChangeCheck();
            mode = (UIElements.Resource.PackIdentifierField.PackIdentifierMode)EditorGUI.EnumPopup(position, mode);
            if (EditorGUI.EndChangeCheck())
            {
                switch (mode)
                {
                    case UIElements.Resource.PackIdentifierField.PackIdentifierMode.id:
                    {
                        value.identifier ??= Identifier.empty;
                        break;
                    }
                    case UIElements.Resource.PackIdentifierField.PackIdentifierMode.path:
                    {
                        value.path ??= RuniPath.empty;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return value;
        }
    }
}