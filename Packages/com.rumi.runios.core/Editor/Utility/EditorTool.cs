#nullable enable
using RuniOS.Editor.Localizations;
using RuniOS.Localizations;
using System;
using UnityEditor;

namespace RuniOS.Editor
{
    [InitializeOnLoad]
    public static partial class EditorTool
    {
        struct EditorLocalizationBridge : IEditorLocalizationBridge
        {
            public Action? onLanguageUpdate
            {
                get => EditorLocalization._onLanguageUpdate;
                set => EditorLocalization._onLanguageUpdate = value;
            }

            public string? GetText(string key, string language = "") => EditorTool.GetText(key, language);
        }
        
        static EditorTool() => RuniOS.Localizations.EditorLocalizationBridge.bridge = new EditorLocalizationBridge();

        /*static EditorTool() => Selection.selectionChanged += ClearCache;

        static void ClearCache() => usePropertyAnimArraySerializedProperty.Clear();*/
    }
}
