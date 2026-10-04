#nullable enable
using UnityEngine;

namespace RuniOS.Editor.Installer.Languages
{
    class LanguageScriptableObject : ScriptableObject
    {
        public SerializableDictionary<string, string> texts = new SerializableDictionary<string, string>();
    }
}
