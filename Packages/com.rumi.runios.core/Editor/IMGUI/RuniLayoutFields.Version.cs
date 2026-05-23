#nullable enable

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static Version VersionField(Version value) => VersionField(GUIContent.none, value);
        public static Version VersionField(string label, Version value) => VersionField(new GUIContent(label), value);
        public static Version VersionField(GUIContent label, Version value) => RuniFields.VersionField(GetMultiColumnsControlRect(label), label, value);
    }
}
