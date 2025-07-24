#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuniEngine.Editor.Localizations
{
    public static class EditorLocalization
    {
        public static string currentLanguage => EditorLanguageConfigAsset.currentLanguage;

        public static Dictionary<string, List<EditorLanguageDataAsset>> registeredDataAssets = new();

        public static void RegisterLanguage(params EditorLanguageDataAsset?[] dataAssets)
        {
            for (int i = 0; i < dataAssets.Length; i++)
            {
                EditorLanguageDataAsset? dataAsset = dataAssets[i];
                if (dataAsset == null)
                    continue;

                List<EditorLanguageDataAsset> dataList;
                if (registeredDataAssets.ContainsKey(dataAsset.languageKey))
                    dataList = registeredDataAssets[dataAsset.languageKey];
                else
                    registeredDataAssets.Add(dataAsset.languageKey, dataList = new List<EditorLanguageDataAsset>());

                if (!dataList.Contains(dataAsset))
                    dataList.Add(dataAsset);
            }
        }

        public static IEnumerable<Dictionary<string, string>> GetLanguageDictionarys(string languageKey = "")
        {
            if (string.IsNullOrEmpty(languageKey))
                languageKey = currentLanguage;

            if (registeredDataAssets.Count <= 0)
            {
                foreach (var item in ReflectionUtility.types.Where(static x => typeof(ScriptableObject).IsAssignableFrom(x) && typeof(IEditorLocalizationRegister).IsAssignableFrom(x)))
                    ScriptableObject.CreateInstance(item);
            }

            if (registeredDataAssets.TryGetValue(languageKey, out var datas))
                return datas.Select(x => x._languages);

            return Enumerable.Empty<Dictionary<string, string>>();
        }
    }
}
