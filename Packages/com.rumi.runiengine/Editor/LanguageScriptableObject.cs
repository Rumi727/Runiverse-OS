#nullable enable
using UnityEngine;

namespace RuniEngine.Installer
{
    //[CreateAssetMenu(fileName = "LanguageScriptableObject", menuName = "Scriptable Objects/LanguageScriptableObject")]
    class LanguageScriptableObject : ScriptableObject
    {
        public SerializableDictionary<string, string> texts = new SerializableDictionary<string, string>();
    }
}
