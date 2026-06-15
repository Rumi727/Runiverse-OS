#nullable enable
using RuniOS.Editor.Localizations;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
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
    }
}