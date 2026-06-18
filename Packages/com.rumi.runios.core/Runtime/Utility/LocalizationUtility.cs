#nullable enable
using RuniOS.Formats;
using RuniOS.Resource;
using RuniOS.Resource.Languages;
using System.Collections.Concurrent;

namespace RuniOS.Utility
{
    public static class LocalizationUtility
    {
        static LocalizationUtility() => ResourceManager.preReloadCompletionEvent += cachedParseResult.Clear;

        static readonly ConcurrentDictionary<(Identifier identifier, string languageCode), IReadOnlyList<CompositeFormatSegment>> cachedParseResult = [];

        /// <summary>
        /// Retrieves the localized text for the given identifier and language code.<br/>
        /// 주어진 식별자와 언어 코드에 대한 지역화된 텍스트를 검색합니다.
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier (key) for the localized text.<br/>
        /// 지역화된 텍스트의 고유 식별자(키).
        /// </param>
        /// <param name="languageCode">
        /// The specific language code to look up. <see langword="null"/> uses the system default.<br/>
        /// 조회할 특정 언어 코드. <see langword="null"/>은 시스템 기본값을 사용합니다.
        /// </param>
        /// <returns>
        /// The localized text if found; otherwise, <see langword="null"/>.<br/>
        /// 찾은 경우 지역화된 텍스트, 그렇지 않으면 <see langword="null"/>입니다.
        /// </returns>
        public static string? GetText(Identifier identifier, string? languageCode = null)
        {
            languageCode ??= "ko_kr"; /* TODO : 이거 바꿔라 */

            LanguageAssetRegistry? registry = AssetRegistryManager.Get<LanguageAssetRegistry>();
            IReadOnlyDictionary<string, string>? asset = registry?.GetAsset(new Identifier(identifier.nameSpace, languageCode));

            return asset?.GetValueOrDefault(identifier.path.value);
        }

        public static IReadOnlyList<CompositeFormatSegment>? GetFormatSegments(Identifier identifier, string? languageCode = null)
        {
            languageCode ??= "ko_kr"; /* TODO : 이거 바꿔라 */

            return cachedParseResult.GetOrAdd((identifier, languageCode), x =>
            {
                string? value = GetText(x.identifier, x.languageCode);
                return CompositeFormat.Parse(value ?? x.identifier);
            });
        }

        public static IEnumerable<string> GetAllLanguageCode()
        {
            LanguageAssetRegistry? registry = AssetRegistryManager.Get<LanguageAssetRegistry>();
            return registry?.GetAllLanguageCodes() ?? [];
        }
    }
}