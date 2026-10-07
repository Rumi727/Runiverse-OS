#nullable enable
using RuniOS.Resource;

namespace RuniOS.Editor.Localizations
{
    [InitializeOnLoad]
    public static class EditorLocalization
    {
        static EditorLocalization() => ResourceManager.reloadCompletionEvent += () => _onLanguageUpdate?.Invoke();

        public static string currentLanguage
        {
            get => EditorLanguageConfigAsset.currentLanguage;
            set => EditorLanguageConfigAsset.currentLanguage = value;
        }

        internal static Action? _onLanguageUpdate;
        public static event Action? onLanguageUpdate
        {
            add => _onLanguageUpdate += value;
            remove => _onLanguageUpdate -= value;
        }

        public static string? GetText(Identifier identifier, string? language = "")
        {
            if (string.IsNullOrEmpty(language))
                language = currentLanguage;

            return LocalizationUtility.GetText(identifier, language);
        }
    }
}