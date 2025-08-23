#nullable enable
using System;

namespace RuniOS.Localizations
{
    interface IEditorLocalizationBridge
    {
        Action? onLanguageUpdate { get; set; }
        
        string? GetText(string key, string language = "");
    }
}