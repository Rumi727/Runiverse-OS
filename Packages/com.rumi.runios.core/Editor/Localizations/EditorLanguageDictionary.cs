#nullable enable
using RuniOS.Collections;
using RuniOS.Collections.Generic;
using System.Collections;

namespace RuniOS.Editor.Localizations
{
    [Serializable]
    class EditorLanguageDictionary : Dictionary<string, string>, ISerializableDictionary<string, string, EditorLanguageSerializableKeyValuePair>, ISerializableDictionary
    {
        [SerializeField] List<EditorLanguageSerializableKeyValuePair> pairs = new();

        IList<EditorLanguageSerializableKeyValuePair> ISerializableDictionary<string, string, EditorLanguageSerializableKeyValuePair>.pairs => pairs;
        IList ISerializableDictionary.pairs => pairs;
    }
}