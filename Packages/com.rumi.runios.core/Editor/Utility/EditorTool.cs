#nullable enable
namespace RuniOS.Editor
{
    [InitializeOnLoad]
    public static partial class EditorTool
    {
        static EditorTool() => _ = projectPath;

        /*struct EditorLocalizationBridge : IEditorLocalizationBridge
        {
            public Action? onLanguageUpdate
            {
                get => EditorLocalization._onLanguageUpdate;
                set => EditorLocalization._onLanguageUpdate = value;
            }

            public string? GetText(string key, string language = "") => EditorTool.GetText(key, language);
        }

        static EditorTool() => RuniOS.Localizations.EditorLocalizationBridge.bridge = new EditorLocalizationBridge();*/

        /*static EditorTool() => Selection.selectionChanged += ClearCache;

        static void ClearCache() => usePropertyAnimArraySerializedProperty.Clear();*/
    }
}