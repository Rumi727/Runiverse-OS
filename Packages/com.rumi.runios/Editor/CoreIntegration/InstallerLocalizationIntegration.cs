#nullable enable
using RuniOS.Editor.Localizations;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;

namespace RuniOS.Editor.Installer
{
    static partial class InstallerLocalizationIntegration
    {
        [OnAssemblyLoaded]
        static void OnAssemblyLoaded()
        {
            InstallerLocalization.languageChanged += OnInstallerLanguageChanged;
            EditorLocalization.onLanguageUpdate += OnEditorLanguageChanged;

            // Read the existing Editor locale after asset import, as the legacy window did on opening.
            EditorApplication.delayCall += OnEditorLanguageChanged;
        }

        [OnAssemblyUnloading]
        static void OnAssemblyUnloading()
        {
            InstallerLocalization.languageChanged -= OnInstallerLanguageChanged;
            EditorLocalization.onLanguageUpdate -= OnEditorLanguageChanged;
        }

        static void OnInstallerLanguageChanged()
        {
            string language = InstallerLocalization.currentLanguage;
            if (EditorLocalization.currentLanguage != language)
                EditorLocalization.currentLanguage = language;
        }

        static void OnEditorLanguageChanged()
        {
            string language = EditorLocalization.currentLanguage;
            if (InstallerLocalization.currentLanguage != language)
                InstallerLocalization.currentLanguage = language;
        }
    }
}
