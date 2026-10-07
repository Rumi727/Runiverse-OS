#nullable enable
using System.Globalization;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static char CharField(Rect position, char value) => DoCharField(position, value);
        public static char CharField(Rect position, string label, char value) => CharField(position, new GUIContent(label), value);
        public static char CharField(Rect position, GUIContent label, char value) => DoCharField(EditorGUI.PrefixLabel(position, label), value);

        static char DoCharField(Rect position, char value)
        {
            string stringValue;
            switch (value)
            {
                case '\n':
                    stringValue = "\\n";
                    break;
                case '\r':
                    stringValue = "\\r";
                    break;
                case '\t':
                    stringValue = "\\t";
                    break;
                case '\v':
                    stringValue = "\\v";
                    break;
                case '\0':
                    stringValue = "\\0";
                    break;
                case '\a':
                    stringValue = "\\a";
                    break;
                case '\b':
                    stringValue = "\\b";
                    break;
                case '\f':
                    stringValue = "\\f";
                    break;
                default:
                {
                    if (char.IsControl(value))
                        stringValue = $"\\u{(int)value:X4}";
                    else
                        stringValue = value.ToString();
                    break;
                }
            }

            BeginIndentLevel(0);

            EditorGUI.BeginChangeCheck();
            stringValue = EditorGUI.TextField(position, stringValue);
            if (EditorGUI.EndChangeCheck())
            {
                if (stringValue.StartsWith("\\u", StringComparison.Ordinal))
                {
                    if (stringValue.Length == 6 && uint.TryParse(stringValue.Substring(2), NumberStyles.HexNumber, null, out uint result))
                        return (char)result;
                }
                else switch (stringValue)
                {
                    case "\\n":
                        return '\n';
                    case "\\r":
                        return '\r';
                    case "\\t":
                        return '\t';
                    case "\\v":
                        return '\v';
                    case "\\0":
                        return '\0';
                    case "\\a":
                        return '\a';
                    case "\\b":
                        return '\b';
                    case "\\f":
                        return '\f';
                    default:
                    {
                        if (char.TryParse(stringValue, out char result))
                            return result;
                        break;
                    }
                }
            }

            EndIndentLevel();

            return value;
        }
    }
}
