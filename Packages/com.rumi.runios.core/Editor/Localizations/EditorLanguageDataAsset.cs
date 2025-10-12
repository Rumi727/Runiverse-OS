#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace RuniOS.Editor.Localizations
{
    [CreateAssetMenu(fileName = "RuniOS Editor Language Asset", menuName = "Scriptable Objects/RuniOS Editor Language Asset")]
    public sealed class EditorLanguageDataAsset : ScriptableObject
    {
        public string languageKey => _languageKey;
        public IReadOnlyDictionary<string, string> languages => _languages;

        [SerializeField] string _languageKey = "";
        [SerializeField] internal EditorLanguageDictionary _languages = new EditorLanguageDictionary();
    }
}
