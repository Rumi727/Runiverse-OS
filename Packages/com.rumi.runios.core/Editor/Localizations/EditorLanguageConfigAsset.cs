#nullable enable
using UnityEngine;

namespace RuniOS.Editor.Localizations
{
    public sealed class EditorLanguageConfigAsset : RuniOSConfigObject<EditorLanguageConfigAsset>
    {
        public static string currentLanguage => instance._currentLanguage;
        [SerializeField] string _currentLanguage = "en_us";
    }
}
