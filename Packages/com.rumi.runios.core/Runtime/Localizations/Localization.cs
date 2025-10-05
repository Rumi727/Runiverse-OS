#nullable enable
using RuniOS.Resource;
using RuniOS.Resource.Languages;
using System.Collections.Generic;

namespace RuniOS.Localizations
{
    public static class Localization
    {
        public static string GetTextOrKey(string key, string language = "") => GetText(key, language) ?? key;
        public static string? GetText(string key, string language = "")
        {
            LanguageAssetRegistry? registry = ResourceManager.GetRegistry<LanguageAssetRegistry>();
            return registry?.preloadedAsset.GetValueOrDefault(language)?.GetValueOrDefault(key);
        }
    }
}