#nullable enable

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static RectOffset RectOffsetField(RectOffset value) => RectOffsetField(GUIContent.none, value);
        public static RectOffset RectOffsetField(string label, RectOffset value) => RectOffsetField(new GUIContent(label), value);
        public static RectOffset RectOffsetField(GUIContent label, RectOffset value) => RuniFields.RectOffsetField(GetMultiColumnsControlRect(label), label, value);
    }
}
