#nullable enable
using UnityEngine;

namespace RuniOS.Installer
{
    //[CreateAssetMenu(fileName = "LanguageScriptableObject", menuName = "Scriptable Objects/LanguageScriptableObject")]
    class LanguageScriptableObject : ScriptableObject
    {
        public SerializableDictionary<string, string> texts = new SerializableDictionary<string, string>();
    }
}
