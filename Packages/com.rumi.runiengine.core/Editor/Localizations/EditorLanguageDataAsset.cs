#nullable enable
using RuniOS.Collections;
using UnityEngine;

namespace RuniOS.Editor.Localizations
{
    [CreateAssetMenu(fileName = "RuniOS Editor Language Asset", menuName = "Scriptable Objects/RuniOS Editor Language Asset")]
    public sealed class EditorLanguageDataAsset : ScriptableObject
    {
        public string languageKey => _languageKey;
        public ISerializableDictionary languages => _languages;

        [SerializeField] internal string _languageKey = "";
        [SerializeField] internal EditorLanguageDictionary _languages = new EditorLanguageDictionary();
    }
}
