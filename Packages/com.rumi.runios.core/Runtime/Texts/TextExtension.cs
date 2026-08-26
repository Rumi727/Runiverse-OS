#nullable enable
using RuniOS.Resource;
using RuniOS.Texts.Builders.RichTexts;
using System.ComponentModel;
using System.Text;

namespace RuniOS.Texts
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static partial class TextExtension
    {
        static readonly Text _empty = new EmptyText();

        extension(Text text)
        {
            /// <summary>
            /// Gets a shared text instance that renders no content.<br/>
            /// 아무 콘텐츠도 렌더링하지 않는 공유 텍스트 인스턴스를 가져옵니다.
            /// </summary>
            public static Text empty => _empty;

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
            /// Creates an empty literal text instance.<br/>
            /// 빈 리터럴 텍스트 인스턴스를 만듭니다.
            /// </summary>
            /// <returns>
            /// A new empty <see cref="LiteralText"/> instance.<br/>
            /// 새 빈 <see cref="LiteralText"/> 인스턴스를 반환합니다.
            /// </returns>
            public static LiteralText Literal() => new LiteralText();

            /// <summary>
            /// Creates a literal text value.<br/>
            /// 리터럴 텍스트 값을 만듭니다.
            /// </summary>
            /// <param name="value">
            /// The literal string represented by the text.<br/>
            /// 텍스트가 나타낼 리터럴 문자열입니다.
            /// </param>
            /// <returns>
            /// A text instance that renders <paramref name="value"/> directly.<br/>
            /// <paramref name="value"/>를 직접 렌더링하는 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public static LiteralText Literal(string value) => new LiteralText(value);

            /// <summary>
            /// Creates a <see cref="ValueText{T}"/> instance that contains a <see langword="null"/> value.<br/>
            /// <see langword="null"/> 값을 담은 <see cref="ValueText{T}"/> 인스턴스를 만듭니다.
            /// </summary>
            /// <returns>
            /// A new <see cref="ValueText{T}"/> instance that contains a <see langword="null"/> value.<br/>
            /// <see langword="null"/> 값을 담은 새 <see cref="ValueText{T}"/> 인스턴스를 반환합니다.
            /// </returns>
            public static ValueText<object?> Value() => new ValueText<object?>(null);

            /// <summary>
            /// Creates a <see cref="ValueText{T}"/> instance from a value.<br/>
            /// 값에서 <see cref="ValueText{T}"/> 인스턴스를 만듭니다.
            /// </summary>
            /// <param name="value">
            /// The value represented by the text.<br/>
            /// 텍스트가 나타낼 값입니다.
            /// </param>
            /// <returns>
            /// A text instance that renders <paramref name="value"/> directly.<br/>
            /// <paramref name="value"/>를 직접 렌더링하는 텍스트 인스턴스를 반환합니다.
            /// </returns>
            public static ValueText<T> Value<T>(T value) => new ValueText<T>(value);

            /// <summary>
            /// Creates a <see cref="ValueText{T}"/> instance with alignment information.<br/>
            /// 정렬 정보를 가진 <see cref="ValueText{T}"/> 인스턴스를 만듭니다.
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
            /// A <see cref="ValueText{T}"/> instance with alignment information.<br/>
            /// 정렬 정보를 가진 <see cref="ValueText{T}"/> 인스턴스를 반환합니다.
            /// </returns>
            public static ValueText<T> Value<T>(T value, int alignment) => new ValueText<T>(value, alignment);

            /// <summary>
            /// Creates a <see cref="ValueText{T}"/> instance with a format string.<br/>
            /// format 문자열을 가진 <see cref="ValueText{T}"/> 인스턴스를 만듭니다.
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
            /// A <see cref="ValueText{T}"/> instance with format information.<br/>
            /// format 정보를 가진 <see cref="ValueText{T}"/> 인스턴스를 반환합니다.
            /// </returns>
            public static ValueText<T> Value<T>(T value, string format) => new ValueText<T>(value, format);

            /// <summary>
            /// Creates a <see cref="ValueText{T}"/> instance with alignment and format information.<br/>
            /// 정렬 및 format 정보를 가진 <see cref="ValueText{T}"/> 인스턴스를 만듭니다.
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
            /// A <see cref="ValueText{T}"/> instance with alignment and format information.<br/>
            /// 정렬 및 format 정보를 가진 <see cref="ValueText{T}"/> 인스턴스를 반환합니다.
            /// </returns>
            public static ValueText<T> Value<T>(T value, int alignment, string format) => new ValueText<T>(value, alignment, format);

            /// <summary>
            /// Creates an empty localized text value.<br/>
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

            public string ToRichText() => RichTextBuilder.Build(text);
            public void BuildTo(StringBuilder builder) => RichTextBuilder.BuildTo(text, builder);
        }
    }
}