#nullable enable
using RuniEngine.Collections;
using RuniEngine.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RuniEngine.Editor.Localizations
{
    [Serializable]
    class EditorLanguageDictionary : Dictionary<string, string>, ISerializableDictionary<string, string, EditorLanguageSerializableKeyValuePair>, ISerializableDictionary
    {
        [SerializeField] List<EditorLanguageSerializableKeyValuePair> pairs = new();

        IList<EditorLanguageSerializableKeyValuePair> ISerializableDictionary<string, string, EditorLanguageSerializableKeyValuePair>.pairs => pairs;
        IList ISerializableDictionary.pairs => pairs;
    }
}
