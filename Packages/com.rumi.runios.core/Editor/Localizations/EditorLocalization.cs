#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuniOS.Editor.Localizations
{
    public static class EditorLocalization
    {
        public static string currentLanguage
        {
            get => EditorLanguageConfigAsset.currentLanguage;
            set => EditorLanguageConfigAsset.currentLanguage = value;
        }

        internal static Action? _onLanguageUpdate;
        public static event Action? onLanguageUpdate
        {
            add => _onLanguageUpdate += value;
            remove => _onLanguageUpdate -= value;
        }

        static readonly Dictionary<string, List<EditorLanguageDataAsset>> _registeredDataAssets = new();
        public static IReadOnlyDictionary<string, List<EditorLanguageDataAsset>> registeredDataAssets { get; } = _registeredDataAssets.AsReadOnly();

        public static void RegisterLanguage(params EditorLanguageDataAsset?[] dataAssets)
        {
            for (int i = 0; i < dataAssets.Length; i++)
            {
                EditorLanguageDataAsset? dataAsset = dataAssets[i];
                if (dataAsset == null)
                    continue;

                if (!_registeredDataAssets.TryGetValue(dataAsset.languageKey, out List<EditorLanguageDataAsset> dataList))
                    _registeredDataAssets.Add(dataAsset.languageKey, dataList = new List<EditorLanguageDataAsset>());

                if (!dataList.Contains(dataAsset))
                    dataList.Add(dataAsset);
            }
        }

        public static IEnumerable<Dictionary<string, string>> GetLanguageDictionarys(string languageKey = "")
        {
            if (string.IsNullOrEmpty(languageKey))
                languageKey = currentLanguage;

            if (_registeredDataAssets.Count <= 0)
            {
                foreach (var item in ReflectionUtility.types.Where(static x => typeof(ScriptableObject).IsAssignableFrom(x) && typeof(IEditorLocalizationRegister).IsAssignableFrom(x)))
                    ScriptableObject.CreateInstance(item);
            }

            if (_registeredDataAssets.TryGetValue(languageKey, out var datas))
                return datas.Select(static x => x._languages);

            return Enumerable.Empty<Dictionary<string, string>>();
        }
    }
}
