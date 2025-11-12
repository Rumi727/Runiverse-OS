#nullable enable
using System;

namespace RuniOS.Localizations
{
    public static class EditorLocalizationBridge
    {
        internal static IEditorLocalizationBridge? bridge;

        public static event Action onLanguageUpdate
        {
            add
            {
                if (bridge != null)
                    bridge.onLanguageUpdate += value;
            }
            remove
            {
                if (bridge != null)
                    bridge.onLanguageUpdate -= value;
            }
        }

        public static string GetTextOrKey(string key, string language = "") => bridge?.GetText(key, language) ?? key;
        public static string? GetText(string key, string language = "") => bridge?.GetText(key, language);
    }
}