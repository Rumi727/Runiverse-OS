#nullable enable
using RuniOS.Editor.Localizations;
using RuniOS.Resource;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        static readonly GUIContent tempContent = new GUIContent();

        public static string GetTextOrKey(Identifier identifier)
        {
            string? result = GetText(identifier);
            if (result == null)
                return identifier;

            return result;
        }

        public static string? GetText(Identifier key, string? language = "") => EditorLocalization.GetText(key, language);

        public static GUIContent TempContent(string text)
        {
            tempContent.text = text;
            tempContent.tooltip = null;

            return tempContent;
        }

        public static GUIContent TempContent(string text, string? tooltip)
        {
            tempContent.text = text;
            tempContent.tooltip = tooltip;

            return tempContent;
        }

        public static GUIContent TrTempContent(Identifier text) => TempContent(GetTextOrKey(text));

        public static GUIContent TrTempContent(Identifier text, Identifier tooltip) => TempContent(GetTextOrKey(text), GetTextOrKey(tooltip));
    }
}