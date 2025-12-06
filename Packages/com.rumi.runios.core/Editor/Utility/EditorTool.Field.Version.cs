#nullable enable
namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public static Version VersionFieldLayout(Version value) => VersionFieldLayout(GUIContent.none, value);
        public static Version VersionFieldLayout(string label, Version value) => VersionFieldLayout(new GUIContent(label), value);
        public static Version VersionFieldLayout(GUIContent label, Version value) => VersionField(GetMultiColumnsControlRect(label), label, value);

        public static Version VersionField(Rect position, Version value) => DoVersionField(position, value);
        public static Version VersionField(Rect position, string label, Version value) => VersionField(position, new GUIContent(label), value);
        public static Version VersionField(Rect position, GUIContent label, Version value)
        {
            position = DrawMultiColumnsFieldPrefixLabel(position, label, 4);

            BeginIndentLevel(0);
            value = DoVersionField(position, value);
            EndIndentLevel();
            
            return value;
        }

        static Version DoVersionField(Rect position, Version value)
        {
            float fieldWidth = (position.width - (2 * 4) - (4 * 2)) / 3f;

            {
                position.width = fieldWidth;
                value.major = NullablePrimitiveField(position, value.major, "*");
                position.x += position.width + 4;
            }

            {
                position.width = 8;
                position.x -= 4;

                GUI.Label(position, ".");

                position.x += position.width;
                position.width += 4;
            }

            {
                position.width = fieldWidth.Floor();
                value.minor = NullablePrimitiveField(position, value.minor, "*");
                position.x += position.width + 4;
            }

            {
                position.width = 8;
                position.x -= 4;

                GUI.Label(position, ".");

                position.x += position.width;
                position.width += 4;
            }

            {
                position.width = fieldWidth.Ceil();
                value.patch = NullablePrimitiveField(position, value.patch, "*");
                position.x += position.width + 4;
            }
        
            return value;
        }
    }
}