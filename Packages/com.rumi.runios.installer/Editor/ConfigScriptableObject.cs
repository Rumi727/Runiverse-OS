#nullable enable
using System.Collections.Generic;
using RuniOS.PackageManagement.Unity;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Installer
{
    class ConfigScriptableObject : ScriptableObject
    {
        public string currentLanguage = "en_us";
        public List<PackageAsset> selectedRoots = [];

        public static ConfigScriptableObject config
        {
            get
            {
                if (_config != null)
                    return _config;

                const string path = "Assets/Runiverse OS/Installer/SetupConfig.asset";
                ConfigScriptableObject? scriptableObject = AssetDatabase.LoadAssetAtPath<ConfigScriptableObject>(path);
                if (scriptableObject != null)
                    return _config = scriptableObject;

                scriptableObject = CreateInstance<ConfigScriptableObject>();

                if (!AssetDatabase.AssetPathExists("Assets/Runiverse OS"))
                    AssetDatabase.CreateFolder("Assets", "Runiverse OS");

                if (!AssetDatabase.AssetPathExists("Assets/Runiverse OS/Installer"))
                    AssetDatabase.CreateFolder("Assets/Runiverse OS", nameof(Installer));

                if (!AssetDatabase.AssetPathExists(path))
                    AssetDatabase.CreateAsset(scriptableObject, path);

                return _config = scriptableObject;
            }
        }
        static ConfigScriptableObject? _config;

        public new void SetDirty() => EditorUtility.SetDirty(this);
    }
}
