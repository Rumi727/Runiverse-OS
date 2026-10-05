#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.Editor.Installer.Languages;
using UnityEditor;

namespace RuniOS.Editor.Installer
{
    /// <summary>
    /// Shares the Installer locale and translations independently of Setup windows.<br/>
    /// Setup 창과 독립적으로 Installer locale과 번역을 공유합니다.
    /// </summary>
    public static class InstallerLocalization
    {
        /// <summary>
        /// Gets the locales supplied by the existing Installer language assets.<br/>
        /// 기존 Installer 언어 에셋이 제공하는 locale 목록을 가져옵니다.
        /// </summary>
        public static IReadOnlyList<string> languages { get; } = ["en_us", "ko_kr", "ja_jp"];

        static readonly LanguageScriptableObject?[] languageObjects = new LanguageScriptableObject?[languages.Count];

        /// <summary>
        /// Gets or sets the saved Installer locale; a changed value raises <see cref="languageChanged"/>.<br/>
        /// 저장된 Installer locale을 가져오거나 설정합니다. 값이 변경되면 <see cref="languageChanged"/>가 발생합니다.
        /// </summary>
        public static string currentLanguage
        {
            get => ConfigScriptableObject.config.currentLanguage;
            set
            {
                ConfigScriptableObject config = ConfigScriptableObject.config;
                if (config.currentLanguage == value)
                    return;

                config.currentLanguage = value;
                config.SetDirty();
                languageChanged?.Invoke();
            }
        }

        /// <summary>
        /// Occurs after the Installer locale changes.<br/>
        /// Installer locale이 변경된 뒤 발생합니다.
        /// </summary>
        public static event Action? languageChanged;

        /// <summary>
        /// Gets a translation in the current locale, or <paramref name="key"/> when absent.<br/>
        /// 현재 locale의 번역을 가져옵니다. 번역이 없으면 <paramref name="key"/>를 반환합니다.
        /// </summary>
        /// <param name="key">
        /// The existing translation key.<br/>
        /// 기존 번역 key입니다.
        /// </param>
        /// <returns>
        /// The translation, an empty string for a stored <see langword="null"/>, or the missing key.<br/>
        /// 번역을 반환합니다. 저장된 값이 <see langword="null"/>이면 빈 문자열, 번역이 없으면 key를 반환합니다.
        /// </returns>
        public static string GetText(string key) => GetText(key, currentLanguage);

        /// <summary>
        /// Gets a translation in <paramref name="language"/>, retaining the legacy fallback.<br/>
        /// 레거시 fallback을 유지하며 <paramref name="language"/>의 번역을 가져옵니다.
        /// </summary>
        /// <param name="key">
        /// The existing translation key.<br/>
        /// 기존 번역 key입니다.
        /// </param>
        /// <param name="language">
        /// The locale to look up.<br/>
        /// 조회할 locale입니다.
        /// </param>
        /// <returns>
        /// The translation, an empty string for a stored <see langword="null"/>, or the missing key.<br/>
        /// 번역을 반환합니다. 저장된 값이 <see langword="null"/>이면 빈 문자열, 번역이 없으면 key를 반환합니다.
        /// </returns>
        public static string GetText(string key, string language)
        {
            TryGetText(key, language, out string text);
            return text;
        }

        /// <summary>
        /// Looks up a translation in the current locale.<br/>
        /// 현재 locale에서 번역을 조회합니다.
        /// </summary>
        /// <param name="key">
        /// The existing translation key.<br/>
        /// 기존 번역 key입니다.
        /// </param>
        /// <param name="text">
        /// Receives the translation or the legacy fallback.<br/>
        /// 번역 또는 레거시 fallback을 받습니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the key exists; otherwise, <see langword="false"/>.<br/>
        /// key가 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool TryGetText(string key, out string text) => TryGetText(key, currentLanguage, out text);

        /// <summary>
        /// Looks up a translation in <paramref name="language"/>.<br/>
        /// <paramref name="language"/>에서 번역을 조회합니다.
        /// </summary>
        /// <param name="key">
        /// The existing translation key.<br/>
        /// 기존 번역 key입니다.
        /// </param>
        /// <param name="language">
        /// The locale to look up.<br/>
        /// 조회할 locale입니다.
        /// </param>
        /// <param name="text">
        /// Receives the translation, an empty string for a stored <see langword="null"/>, or the missing key.<br/>
        /// 번역을 받습니다. 저장된 값이 <see langword="null"/>이면 빈 문자열, 번역이 없으면 key를 받습니다.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the key exists; otherwise, <see langword="false"/>.<br/>
        /// key가 존재하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public static bool TryGetText(string key, string language, out string text)
        {
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i] != language)
                    continue;

                LanguageScriptableObject? languageObject = languageObjects[i] ??=
                    AssetDatabase.LoadAssetAtPath<LanguageScriptableObject>(
                        "Packages/com.rumi.runios/Editor/Languages/" + language + ".asset");

                if (languageObject != null && languageObject.texts.TryGetValue(key, out string? value))
                {
                    text = value ?? string.Empty;
                    return true;
                }

                break;
            }

            text = key;
            return false;
        }
    }
}
