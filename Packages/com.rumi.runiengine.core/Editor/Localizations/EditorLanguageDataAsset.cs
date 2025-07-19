#nullable enable
using RuniEngine.Collections;
using UnityEngine;

namespace RuniEngine.Editor.Localizations
{
    [CreateAssetMenu(fileName = "Editor Language Asset", menuName = "Scriptable Objects/Language Asset")]
    public sealed class EditorLanguageDataAsset : ScriptableObject
    {
        public string languageKey => _languageKey;
        public ISerializableDictionary languages => _languages;

        [SerializeField] internal string _languageKey = "";
        [SerializeField] internal EditorLanguageDictionary _languages = new EditorLanguageDictionary();
    }
}
