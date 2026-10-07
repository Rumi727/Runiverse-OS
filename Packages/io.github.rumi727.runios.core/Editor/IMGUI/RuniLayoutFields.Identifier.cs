#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static Identifier IdentifierField(Identifier value, Action<Rect>? dropdownAction = null) => IdentifierField(GUIContent.none, value, dropdownAction);
        public static Identifier IdentifierField(string label, Identifier value, Action<Rect>? dropdownAction = null) => IdentifierField(new GUIContent(label), value, dropdownAction);
        public static Identifier IdentifierField(GUIContent label, Identifier value, Action<Rect>? dropdownAction = null) => RuniFields.IdentifierField(GetMultiColumnsControlRect(label), label, value, dropdownAction);
    }
}
