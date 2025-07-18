#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Install
{
    //[CreateAssetMenu(fileName = "LanguageScriptableObject", menuName = "Scriptable Objects/LanguageScriptableObject")]
    class ConfigScriptableObject : ScriptableObject
    {
        public int screenIndex = 0;
        public string currentLanguage = "en_us";

        public static ConfigScriptableObject config
        { 
            get
            {
                if (_config != null)
                    return _config;

                string path = "Assets/Runiverse OS/Installer/Config.asset";
                ConfigScriptableObject? scriptableObject = AssetDatabase.LoadAssetAtPath<ConfigScriptableObject>(path);
                if (scriptableObject != null)
                    return _config = scriptableObject;

                scriptableObject = CreateInstance<ConfigScriptableObject>();

                if (!AssetDatabase.AssetPathExists("Assets/Runiverse OS"))
                    AssetDatabase.CreateFolder("Assets", "Runiverse OS");

                if (!AssetDatabase.AssetPathExists("Assets/Runiverse OS/Installer"))
                    AssetDatabase.CreateFolder("Assets/Runiverse OS", "Installer");

                if (!AssetDatabase.AssetPathExists(path))
                    AssetDatabase.CreateAsset(scriptableObject, path);

                return _config = scriptableObject;
            }
        }
        static ConfigScriptableObject? _config;

        public new void SetDirty() => EditorUtility.SetDirty(this);
    }
}
