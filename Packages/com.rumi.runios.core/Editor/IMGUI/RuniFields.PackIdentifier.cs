#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.IO;
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static PackIdentifier PackIdentifierField(Rect position, PackIdentifier value) => DoPackIdentifierField(position, value);
        public static PackIdentifier PackIdentifierField(Rect position, string label, PackIdentifier value) => PackIdentifierField(position, new GUIContent(label), value);
        public static PackIdentifier PackIdentifierField(Rect position, GUIContent label, PackIdentifier value)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 3);
            return DoPackIdentifierField(position, value);
        }

        static int? packIdentifierFieldLastControlID;
        static RuniPath packIdentifierFieldSelectedValue = RuniPath.empty;
        static PackIdentifier DoPackIdentifierField(Rect position, PackIdentifier value)
        {
            position.width -= 54;
            if (value.path != null)
                value.path = PhysicalPathField(position, value.path.Value, true);
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
                    packIdentifierFieldSelectedValue = (RuniPath)x.value;
                };

                if (packIdentifierFieldLastControlID == lastControlID)
                {
                    value.identifier = new Identifier(value.identifier.Value.nameSpace, packIdentifierFieldSelectedValue);

                    packIdentifierFieldSelectedValue = RuniPath.empty;
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
                        value.path ??= PhysicalPath.currentDirectory;
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
