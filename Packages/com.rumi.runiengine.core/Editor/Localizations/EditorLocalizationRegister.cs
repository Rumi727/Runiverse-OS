#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor.Localizations
{
    sealed class EditorLocalizationRegister : ScriptableObject, IEditorLocalizationRegister
    {
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
