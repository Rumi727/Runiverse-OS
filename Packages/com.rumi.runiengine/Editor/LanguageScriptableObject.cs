#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Install
{
    //[CreateAssetMenu(fileName = "LanguageScriptableObject", menuName = "Scriptable Objects/LanguageScriptableObject")]
    class LanguageScriptableObject : ScriptableObject
    {
        public SerializableDictionary<string, string> texts = new SerializableDictionary<string, string>();
    }
}
