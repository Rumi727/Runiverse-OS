#nullable enable
using RuniOS.Resource;
using RuniOS.Resource.Languages;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RuniOS.Localizations
{
    [Serializable]
    public struct Localization
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> struct.<br/>
        /// 새 <see cref="Localization"/> 구조체의 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier (key) used to look up the localized text in the resource registry.<br/>
        /// 리소스 레지스트리에서 지역화된 텍스트를 조회하는 데 사용되는 고유 식별자(키).
        /// </param>
        /// <param name="languageCode">
        /// The specific language code (e.g., "ko_kr", "en_us"). <see langword="null"/> to use the default language.<br/>
        /// 특정 언어 코드 (예: "ko_kr", "en_us"). <see langword="null"/>은 기본 언어를 사용합니다.
        /// </param>
        public Localization(Identifier identifier, string? languageCode = null)
        {
            this.identifier = identifier;
            _languageCode = languageCode;
            
            _replaces = ImmutableArray<PlaceholderReplacePair>.Empty;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> struct with initial replacements.<br/>
        /// 초기 대체 쌍(replacements)을 사용하여 새 <see cref="Localization"/> 구조체의 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier (key) used to look up the localized text.<br/>
        /// 지역화된 텍스트를 조회하는 데 사용되는 고유 식별자(키).
        /// </param>
        /// <param name="languageCode">
        /// The specific language code. <see langword="null"/> to use the default language.<br/>
        /// 특정 언어 코드. <see langword="null"/>은 기본 언어를 사용합니다.
        /// </param>
        /// <param name="replaces">
        /// An array of <see cref="PlaceholderReplacePair"/> to apply to the text.<br/>
        /// 텍스트에 적용할 <see cref="PlaceholderReplacePair"/> 배열.
        /// </param>
        public Localization(Identifier identifier, string? languageCode, params PlaceholderReplacePair[] replaces)
        {
            this.identifier = identifier;
            _languageCode = languageCode;
            
            _replaces = replaces.ToImmutableArray();
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> struct with initial replacements.<br/>
        /// 초기 대체 쌍(replacements)을 사용하여 새 <see cref="Localization"/> 구조체의 인스턴스를 초기화합니다.<br/>
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier (key) used to look up the localized text.<br/>
        /// 지역화된 텍스트를 조회하는 데 사용되는 고유 식별자(키).
        /// </param>
        /// <param name="languageCode">
        /// The specific language code. <see langword="null"/> to use the default language.<br/>
        /// 특정 언어 코드. <see langword="null"/>은 기본 언어를 사용합니다.
        /// </param>
        /// <param name="replaces">
        /// A collection of <see cref="PlaceholderReplacePair"/> to apply to the text.<br/>
        /// 텍스트에 적용할 <see cref="PlaceholderReplacePair"/> 컬렉션.
        /// </param>
        public Localization(Identifier identifier, string? languageCode, IEnumerable<PlaceholderReplacePair> replaces)
        {
            this.identifier = identifier;
            _languageCode = languageCode;
            
            _replaces = replaces.ToImmutableArray();
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> struct with initial replacements.<br/>
        /// 초기 대체 쌍(replacements)을 사용하여 새 <see cref="Localization"/> 구조체의 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="identifier">
        /// The unique identifier (key) used to look up the localized text.<br/>
        /// 지역화된 텍스트를 조회하는 데 사용되는 고유 식별자(키).)
        /// </param>
        /// <param name="languageCode">
        /// The specific language code. <see langword="null"/> to use the default language.<br/>
        /// 특정 언어 코드. <see langword="null"/>은 기본 언어를 사용합니다.
        /// </param>
        /// <param name="replaces">
        /// An immutable array of <see cref="PlaceholderReplacePair"/> to apply to the text.<br/>
        /// 텍스트에 적용할 <see cref="PlaceholderReplacePair"/>의 변경 불가능한 배열.
        /// </param>
        public Localization(Identifier identifier, string? languageCode, ImmutableArray<PlaceholderReplacePair> replaces)
        {
            this.identifier = identifier;
            _languageCode = languageCode;
            
            _replaces = replaces;
        }

        /// <summary>
        /// Gets an empty <see cref="Localization"/> instance.<br/>
        /// 비어 있는 <see cref="Localization"/> 인스턴스를 가져옵니다.
        /// </summary>
        public static Localization empty => new Localization();
        
        /// <summary>
        /// The unique identifier (key) for the localized text to be looked up in the resource registry.<br/>
        /// 리소스 레지스트리에서 조회할 지역화된 텍스트의 고유 식별자(키).
        /// </summary>
        [SerializeField] public Identifier identifier;

        /// <summary>
        /// Gets or sets the specific language code for the text. <br/> A <see langword="null"/> or empty value indicates that the default language should be used.<br/>
        /// 텍스트에 대한 특정 언어 코드를 가져오거나 설정합니다. <br/> <see langword="null"/> 또는 빈 값은 기본 언어를 사용해야 함을 나타냅니다.
        /// </summary>
        public string? languageCode
        {
            readonly get => string.IsNullOrEmpty(_languageCode) ? null : _languageCode;
            set => _languageCode = value;
        }
        [SerializeField] string? _languageCode;
        
        /// <summary>
        /// Gets or sets the immutable array of replacement pairs to be applied to the localized text.<br/>
        /// 지역화된 텍스트에 적용할 대체 쌍의 변경 불가능한 배열을 가져오거나 설정합니다.
        /// </summary>
        public ImmutableArray<PlaceholderReplacePair> replaces
        {
            readonly get => _replaces.IsDefault ? ImmutableArray<PlaceholderReplacePair>.Empty : _replaces;
            set => _replaces = value;
        }
        ImmutableArray<PlaceholderReplacePair> _replaces;

        /// <summary>
        /// Retrieves the localized text for the identifier and language code, and then applies all defined replacements.<br/>
        /// The replacement works by matching the <see cref="PlaceholderReplacePair.oldValue"/> as a placeholder key 
        /// (e.g., matching "value" to "{value}") in the localized text.
        /// <br/><br/>
        /// 식별자와 언어 코드에 해당하는 지역화된 텍스트를 검색한 다음 정의된 모든 대체 쌍을 적용합니다.<br/>
        /// 대체는 지역화된 텍스트에서 <see cref="PlaceholderReplacePair.oldValue"/>를 플레이스홀더 키 (예: "value"를 "{value}"에 일치)로 간주하여 일치시킵니다.
        /// </summary>
        /// <returns>
        /// The localized and replaced text.
        /// <br/>
        /// 지역화되고 대체가 적용된 텍스트.
        /// </returns>
        public override readonly string ToString() => replaces.Aggregate(GetTextOrKey(identifier, languageCode), (current, replace) => replace.ReplaceAsPlaceholder(current));
        
        /// <summary>
        /// Retrieves the localized and replaced text, then formats it using C#'s composite formatting feature.<br/>
        /// 지역화되고 대체된 텍스트를 검색한 다음 C#의 복합 포맷팅 기능을 사용하여 서식을 지정합니다.
        /// </summary>
        /// <param name="args">
        /// An object array that contains zero or more objects to format.<br/>
        /// 서식을 지정할 0개 이상의 객체를 포함하는 객체 배열.
        /// </param>
        /// <returns>
        /// The fully formatted string.<br/>
        /// 완전히 서식이 지정된 문자열.
        /// </returns>
        /// <exception cref="FormatException">
        /// The format specification in the resulting string is invalid. <br/> -or- <br/> The index of a format item is less than zero, or greater than or equal to the number of objects in the <paramref name="args"/> array.
        /// <br/><br/>
        /// 결과 문자열의 서식 지정이 잘못되었습니다. <br/> -또는- <br/> 서식 항목의 인덱스가 0보다 작거나 <paramref name="args"/> 배열의 객체 수보다 크거나 같습니다.</exception>
        public readonly string ToFormat(params object[] args) => string.Format(CultureInfo.InvariantCulture, ToString(), args);

        /// <summary>
        /// Creates a new <see cref="Localization"/> instance by adding a new replacement pair.<br/>
        /// 새 대체 쌍을 추가하여 새 <see cref="Localization"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="oldValue">
        /// The placeholder key (e.g., "name") to be replaced in the localized text.<br/>
        /// 지역화된 텍스트에서 대체될 플레이스홀더 키 (예: "name").
        /// </param>
        /// <param name="newValue">
        /// The value to replace the placeholder key with.<br/>
        /// 플레이스홀더 키를 대체할 값.
        /// </param>
        /// <returns>
        /// A new <see cref="Localization"/> instance with the added replacement pair.<br/>
        /// 추가된 대체 쌍이 포함된 새 <see cref="Localization"/> 인스턴스.
        /// </returns>
        public readonly Localization AddReplace(string oldValue, string newValue) => new Localization(identifier, languageCode, replaces.Add(new PlaceholderReplacePair(oldValue, newValue)));
        
        /// <summary>
        /// Creates a new <see cref="Localization"/> instance by adding a new replacement pair.<br/>
        /// 새 대체 쌍을 추가하여 새 <see cref="Localization"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="replace">
        /// The <see cref="PlaceholderReplacePair"/> to add.<br/>
        /// 추가할 <see cref="PlaceholderReplacePair"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="Localization"/> instance with the added replacement pair.<br/>
        /// 추가된 대체 쌍이 포함된 새 <see cref="Localization"/> 인스턴스.
        /// </returns>
        public readonly Localization AddReplace(PlaceholderReplacePair replace) => new Localization(identifier, languageCode, replaces.Add(replace));
        
        /// <summary>
        /// Creates a new <see cref="Localization"/> instance by adding multiple replacement pairs.<br/>
        /// 여러 대체 쌍을 추가하여 새 <see cref="Localization"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="replaces">
        /// An array of <see cref="PlaceholderReplacePair"/> to add.<br/>
        /// 추가할 <see cref="PlaceholderReplacePair"/> 배열.
        /// </param>
        /// <returns>
        /// A new <see cref="Localization"/> instance with the added replacement pairs.<br/>
        /// 추가된 대체 쌍이 포함된 새 <see cref="Localization"/> 인스턴스.
        /// </returns>
        public readonly Localization AddReplace(params PlaceholderReplacePair[] replaces) => new Localization(identifier, languageCode, this.replaces.AddRange(replaces));
        
        /// <summary>
        /// Creates a new <see cref="Localization"/> instance by adding multiple replacement pairs.<br/>
        /// 여러 대체 쌍을 추가하여 새 <see cref="Localization"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="replaces">
        /// A collection of <see cref="PlaceholderReplacePair"/> to add.<br/>
        /// 추가할 <see cref="PlaceholderReplacePair"/> 컬렉션.
        /// </param>
        /// <returns>
        /// A new <see cref="Localization"/> instance with the added replacement pairs.<br/>
        /// 추가된 대체 쌍이 포함된 새 <see cref="Localization"/> 인스턴스.
        /// </returns>
        public readonly Localization AddReplace(IEnumerable<PlaceholderReplacePair> replaces) => new Localization(identifier, languageCode, this.replaces.AddRange(replaces));
        
        /// <summary>
        /// Creates a new <see cref="Localization"/> instance by adding multiple replacement pairs.<br/>
        /// 여러 대체 쌍을 추가하여 새 <see cref="Localization"/> 인스턴스를 생성합니다.
        /// </summary>
        /// <param name="replaces">
        /// An immutable array of <see cref="PlaceholderReplacePair"/> to add.<br/>
        /// 추가할 <see cref="PlaceholderReplacePair"/>의 변경 불가능한 배열.
        /// </param>
        /// <returns>
        /// A new <see cref="Localization"/> instance with the added replacement pairs.<br/>
        /// 추가된 대체 쌍이 포함된 새 <see cref="Localization"/> 인스턴스.
        /// </returns>
        public readonly Localization AddReplace(ImmutableArray<PlaceholderReplacePair> replaces) => new Localization(identifier, languageCode, this.replaces.AddRange(replaces));

        /// <summary>
        /// Attempts to retrieve the localized text for the given identifier and language code.<br/>
        /// If the text is not found, the identifier itself is returned.
        /// <br/><br/>
        /// 주어진 식별자와 언어 코드에 대한 지역화된 텍스트를 검색하려고 시도합니다.<br/>
        /// 텍스트를 찾을 수 없으면 식별자 자체가 반환됩니다.
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
        /// The localized text if found; otherwise, the <paramref name="identifier"/> string.<br/>
        /// 찾은 경우 지역화된 텍스트, 그렇지 않으면 <paramref name="identifier"/> 문자열.
        /// </returns>
        public static string GetTextOrKey(Identifier identifier, string? languageCode = null) => GetText(identifier, languageCode) ?? identifier;
        
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
            LanguageAssetRegistry? registry = ResourceManager.GetRegistry<LanguageAssetRegistry>();
            return registry?.calculatedAsset.GetValueOrDefault(languageCode ?? string.Empty /* TODO : 이거 바꿔라 */)?.GetValueOrDefault(identifier);
        }

        /// <summary>
        /// Implicitly converts a <see cref="Localization"/> to its <see cref="identifier"/>.<br/>
        /// <see cref="Localization"/>을 <see cref="identifier"/>로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">The <see cref="Localization"/> instance.<br/>
        /// <see cref="Localization"/> 인스턴스.
        /// </param>
        public static implicit operator Identifier(Localization value) => value.identifier;
        
        /// <summary>
        /// Implicitly converts an <see cref="Identifier"/> to a <see cref="Localization"/> instance.<br/>
        /// <see cref="Identifier"/>를 <see cref="Localization"/> 인스턴스로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">
        /// The <see cref="Identifier"/> instance.<br/>
        /// <see cref="Identifier"/> 인스턴스.
        /// </param>
        public static implicit operator Localization(Identifier value) => new Localization(value);
        
        /// <summary>
        /// Implicitly converts a <see cref="string"/> value to a <see cref="Localization"/> instance by treating the string as an <see cref="Identifier"/>.<br/>
        /// 문자열 값을 <see cref="Identifier"/>로 처리하여 <see cref="Localization"/> 인스턴스로 암시적으로 변환합니다.
        /// </summary>
        /// <param name="value">
        /// The string value to use as the identifier.<br/>
        /// 식별자로 사용할 문자열 값.
        /// </param>
        public static implicit operator Localization(string value) => new Localization(value);
    }
}