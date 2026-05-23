#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static PackIdentifier PackIdentifierField(PackIdentifier value) => PackIdentifierField(GUIContent.none, value);
        public static PackIdentifier PackIdentifierField(string label, PackIdentifier value) => PackIdentifierField(new GUIContent(label), value);
        public static PackIdentifier PackIdentifierField(GUIContent label, PackIdentifier value) => RuniFields.PackIdentifierField(GetMultiColumnsControlRect(label), label, value);
    }
}
