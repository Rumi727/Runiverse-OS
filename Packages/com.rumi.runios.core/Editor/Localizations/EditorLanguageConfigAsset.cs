#nullable enable
namespace RuniOS.Editor.Localizations;

public sealed class EditorLanguageConfigAsset : RuniOSConfigObject<EditorLanguageConfigAsset>
{
    public static string currentLanguage
    {
        get => instance._currentLanguage;
        set
        {
            instance._currentLanguage = value;
            instance.SetDirty();

            EditorLocalization._onLanguageUpdate?.Invoke();
        }
    }
    [SerializeField] string _currentLanguage = "en_us";
        
    void OnValidate() => EditorLocalization._onLanguageUpdate?.Invoke();
}