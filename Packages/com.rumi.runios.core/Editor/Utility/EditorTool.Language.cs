#nullable enable
using RuniOS.Editor.Localizations;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        static readonly GUIContent tempContent = new GUIContent();

        public static string GetTextOrKey(string key)
        {
            string? result = GetText(key);
            if (result == null)
                return key;

            return result;
        }

        public static string? GetText(string key, string language = "")
        {
            foreach (var item in EditorLocalization.GetLanguageDictionarys(language))
            {
                if (item.TryGetValue(key, out string value))
                    return value;
            }

            return null;
        }

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