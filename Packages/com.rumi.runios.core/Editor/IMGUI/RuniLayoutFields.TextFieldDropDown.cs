#nullable enable
namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static string TextFieldDropDown(string value, out bool isPressed) => TextFieldDropDown(GUIContent.none, value, out isPressed);
        public static string TextFieldDropDown(string label, string value, out bool isPressed) => TextFieldDropDown(new GUIContent(label), value, out isPressed);
        public static string TextFieldDropDown(GUIContent label, string value, out bool isPressed) => RuniFields.TextFieldDropDown(EditorGUILayout.GetControlRect(), label, value, out isPressed);
    }
}
