#nullable enable
using RuniEngine.Editor.Localizations;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public static string TryGetText(string key, string lauguage = "")
        {
            string? result = GetText(key, lauguage);
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
