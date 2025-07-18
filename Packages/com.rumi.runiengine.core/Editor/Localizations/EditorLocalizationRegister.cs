#nullable enable
using RuniEngine.Editor.Localizations;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Localizations
{
    sealed class EditorLocalizationRegister : UnityEditor.Editor
    {
        [InitializeOnLoadMethod]
        static void Init() => CreateInstance<EditorLocalizationRegister>();

        [SerializeField] EditorLanguageDataAsset? en_us;
        [SerializeField] EditorLanguageDataAsset? ko_kr;
        [SerializeField] EditorLanguageDataAsset? ja_jp;

        void Awake()
        {
            EditorLocalization.RegisterLanguage(en_us, ko_kr, ja_jp);
            DestroyImmediate(this);
        }
    }
}
