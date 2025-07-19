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
    class EditorLanguageDictionary : Dictionary<string, string>, ISerializableDictionary<string, string>, ISerializableDictionary
    {
        [SerializeField] List<string?> serializableKeys = new List<string?>();
        [SerializeField, TextArea(0, 100)] List<string?> serializableValues = new List<string?>();

        IList<string?> ISerializableDictionary<string, string>.serializableKeys => serializableKeys;
        IList<string?> ISerializableDictionary<string, string>.serializableValues => serializableValues;

        IList ISerializableDictionary.serializableKeys => serializableKeys;
        IList ISerializableDictionary.serializableValues => serializableValues;

        public void OnAfterDeserialize() => ISerializableDictionary<string, string>.Deserialize(this);
        public void OnBeforeSerialize() => ISerializableDictionary<string, string>.Serialize(this);
    }
}
