#nullable enable
using RuniEngine.IO;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor
{
    public abstract class ConfigObject<T> : ScriptableObject where T : ConfigObject<T>
    {
        public static T instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                FilePath assetFolderPath = "Assets/Runiverse OS";
                string assetPath = (assetFolderPath + typeof(T).Name).AddExtension(".asset");

                T? scriptableObject = AssetDatabase.LoadAssetAtPath<T>(assetPath) ?? CreateInstance<T>();

                if (!AssetDatabase.AssetPathExists(assetFolderPath))
                    AssetDatabase.CreateFolder("Assets", "Runiverse OS");

                if (!AssetDatabase.AssetPathExists(assetPath))
                    AssetDatabase.CreateAsset(scriptableObject, assetPath);

                return _instance = scriptableObject;
            }
        }
        static T? _instance;

        public new void SetDirty() => EditorUtility.SetDirty(this);
    }
}
