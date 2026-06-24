#nullable enable
using RuniOS.Resource;
using RuniOS.Texts.Styles;

namespace RuniOS.Texts
{
    /// <summary>
    /// Represents a styled text element that can be resolved or rendered by text pipelines.<br/>
    /// 텍스트 파이프라인에서 해석되거나 렌더링될 수 있는 스타일 적용 텍스트 요소를 나타냅니다.
    /// </summary>
    public abstract partial class Text
    {
        /// <summary>
        /// Gets a shared text instance that renders no content.<br/>
        /// 아무 콘텐츠도 렌더링하지 않는 공유 텍스트 인스턴스를 가져옵니다.
        /// </summary>
        public static Text empty { get; } = new EmptyText();

        /// <summary>
        /// Creates an empty grouped text value.<br/>
        /// 빈 그룹 텍스트 값을 만듭니다.
        /// </summary>
        /// <returns>
        /// A new empty <see cref="GroupText"/> instance.<br/>
        /// 새 빈 <see cref="GroupText"/> 인스턴스를 반환합니다.
        /// </returns>
        public static GroupText Group() => [];

        /// <summary>
        /// Creates a grouped text value from an interpolated string handler.<br/>
        /// 보간 문자열 핸들러에서 그룹 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="handler">
        /// The interpolated string handler that collected text segments.<br/>
        /// 텍스트 세그먼트를 수집한 보간 문자열 핸들러입니다.
        /// </param>
        /// <returns>
        /// The grouped text collected by <paramref name="handler"/>.<br/>
        /// <paramref name="handler"/>가 수집한 그룹 텍스트를 반환합니다.
        /// </returns>
        public static GroupText Group(GroupTextStringHandler handler) => handler.ToGroupText();

        /// <summary>
        /// Creates an empty grouped text value.<br/>
        /// 빈 리터럴 텍스트 값을 만듭니다.
        /// </summary>
        /// <returns>
        /// A new empty <see cref="LiteralText"/> instance.<br/>
        /// 새 빈 <see cref="LiteralText"/> 인스턴스를 반환합니다.
        /// </returns>
        public static LiteralText Literal() => new LiteralText(null);

        /// <summary>
        /// Creates a literal text value.<br/>
        /// 리터럴 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by the text.<br/>
        /// 텍스트가 나타낼 값입니다.
        /// </param>
        /// <returns>
        /// A text instance that renders <paramref name="value"/> directly.<br/>
        /// <paramref name="value"/>를 직접 렌더링하는 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LiteralText Literal(object? value) => new LiteralText(value);

        /// <summary>
        /// Creates a literal text value with alignment information.<br/>
        /// 정렬 정보를 가진 리터럴 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by the text.<br/>
        /// 텍스트가 나타낼 값입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply when formatting.<br/>
        /// format 처리 시 적용할 복합 format 정렬 너비입니다.
        /// </param>
        /// <returns>
        /// A literal text instance with alignment information.<br/>
        /// 정렬 정보를 가진 리터럴 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LiteralText Literal(object? value, int alignment) => new LiteralText(value, alignment);

        /// <summary>
        /// Creates a literal text value with a format string.<br/>
        /// format 문자열을 가진 리터럴 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by the text.<br/>
        /// 텍스트가 나타낼 값입니다.
        /// </param>
        /// <param name="format">
        /// The format string used when <paramref name="value"/> implements <see cref="System.IFormattable"/>.<br/>
        /// <paramref name="value"/>가 <see cref="System.IFormattable"/>을 구현할 때 사용할 format 문자열입니다.
        /// </param>
        /// <returns>
        /// A literal text instance with format information.<br/>
        /// format 정보를 가진 리터럴 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LiteralText Literal(object? value, string format) => new LiteralText(value, format);

        /// <summary>
        /// Creates a literal text value with alignment and format information.<br/>
        /// 정렬 및 format 정보를 가진 리터럴 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by the text.<br/>
        /// 텍스트가 나타낼 값입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply when formatting.<br/>
        /// format 처리 시 적용할 복합 format 정렬 너비입니다.
        /// </param>
        /// <param name="format">
        /// The format string used when <paramref name="value"/> implements <see cref="System.IFormattable"/>.<br/>
        /// <paramref name="value"/>가 <see cref="System.IFormattable"/>을 구현할 때 사용할 format 문자열입니다.
        /// </param>
        /// <returns>
        /// A literal text instance with alignment and format information.<br/>
        /// 정렬 및 format 정보를 가진 리터럴 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LiteralText Literal(object? value, int alignment, string format) => new LiteralText(value, alignment, format);

        /// <summary>
        /// Creates an empty grouped text value.<br/>
        /// 빈 로컬라이징 텍스트 값을 만듭니다.
        /// </summary>
        /// <returns>
        /// A new empty <see cref="LocalizationText"/> instance.<br/>
        /// 새 빈 <see cref="LocalizationText"/> 인스턴스를 반환합니다.
        /// </returns>
        public static LocalizationText Local() => new LocalizationText(Identifier.empty, []);

        /// <summary>
        /// Creates a localized text value without format arguments.<br/>
        /// format 인수 없는 로컬라이징 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="identifier">
        /// The localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자입니다.
        /// </param>
        /// <param name="languageCode">
        /// The language code to use, or <see langword="null"/> to use the current default language.<br/>
        /// 사용할 언어 코드이며, <see langword="null"/>이면 현재 기본 언어를 사용합니다.
        /// </param>
        /// <returns>
        /// A localized text instance.<br/>
        /// 로컬라이징 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LocalizationText Local(Identifier identifier, string? languageCode = "") => new LocalizationText(identifier, [], languageCode);

        /// <summary>
        /// Creates a localized text value with enumerable format arguments.<br/>
        /// 열거 가능한 format 인수를 가진 로컬라이징 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="identifier">
        /// The localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자입니다.
        /// </param>
        /// <param name="args">
        /// The text arguments used by the localized format string.<br/>
        /// 로컬라이징된 format 문자열에서 사용할 텍스트 인수입니다.
        /// </param>
        /// <param name="languageCode">
        /// The language code to use, or <see langword="null"/> to use the current default language.<br/>
        /// 사용할 언어 코드이며, <see langword="null"/>이면 현재 기본 언어를 사용합니다.
        /// </param>
        /// <returns>
        /// A localized text instance.<br/>
        /// 로컬라이징 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LocalizationText Local(Identifier identifier, IEnumerable<Text> args, string? languageCode = "") => new LocalizationText(identifier, args.ToArray(), languageCode);

        /// <summary>
        /// Creates a localized text value with indexed format arguments.<br/>
        /// 인덱싱 가능한 format 인수를 가진 로컬라이징 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="identifier">
        /// The localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자입니다.
        /// </param>
        /// <param name="args">
        /// The text arguments used by the localized format string.<br/>
        /// 로컬라이징된 format 문자열에서 사용할 텍스트 인수입니다.
        /// </param>
        /// <param name="languageCode">
        /// The language code to use, or <see langword="null"/> to use the current default language.<br/>
        /// 사용할 언어 코드이며, <see langword="null"/>이면 현재 기본 언어를 사용합니다.
        /// </param>
        /// <returns>
        /// A localized text instance.<br/>
        /// 로컬라이징 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LocalizationText Local(Identifier identifier, IList<Text> args, string? languageCode = "") => new LocalizationText(identifier, args, languageCode);

        /// <summary>
        /// Creates a localized text value with format arguments.<br/>
        /// format 인수를 가진 로컬라이징 텍스트 값을 만듭니다.
        /// </summary>
        /// <param name="identifier">
        /// The localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자입니다.
        /// </param>
        /// <param name="args">
        /// The text arguments used by the localized format string.<br/>
        /// 로컬라이징된 format 문자열에서 사용할 텍스트 인수입니다.
        /// </param>
        /// <returns>
        /// A localized text instance.<br/>
        /// 로컬라이징 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public static LocalizationText Local(Identifier identifier, params Text[] args) => new LocalizationText(identifier, args);
    }
}
