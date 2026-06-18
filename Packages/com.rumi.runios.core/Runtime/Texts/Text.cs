#nullable enable
using RuniOS.Resource;
using RuniOS.Texts.Styles;

namespace RuniOS.Texts
{
    /// <summary>
    /// Represents a styled text element that can be resolved or rendered by text pipelines.<br/>
    /// 텍스트 파이프라인에서 해석되거나 렌더링될 수 있는 스타일 적용 텍스트 요소를 나타냅니다.
    /// </summary>
    public abstract class Text
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

        /// <summary>
        /// Gets the style assigned to this text, if any.<br/>
        /// 이 텍스트에 할당된 스타일이 있으면 가져옵니다.
        /// </summary>
        public TextStyle? style { get; private set; }

        /// <summary>
        /// Sets the value of the specified style property on this text.<br/>
        /// 이 텍스트에 지정된 스타일 속성 값을 설정합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type stored by the style property.<br/>
        /// 스타일 속성이 저장하는 값 타입입니다.
        /// </typeparam>
        /// <param name="property">
        /// The style property to set.<br/>
        /// 설정할 스타일 속성입니다.
        /// </param>
        /// <param name="value">
        /// The value to assign to the style property.<br/>
        /// 스타일 속성에 할당할 값입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text SetStyle<T>(StyleKey<T> property, T value) where T : notnull
        {
            (style ??= new TextStyle()).Set(property, value);
            return this;
        }

        /// <summary>
        /// Removes the value assigned to the specified style property from this text.<br/>
        /// 이 텍스트에서 지정된 스타일 속성에 할당된 값을 제거합니다.
        /// </summary>
        /// <typeparam name="T">
        /// The value type stored by the style property.<br/>
        /// 스타일 속성이 저장하는 값 타입입니다.
        /// </typeparam>
        /// <param name="property">
        /// The style property to unset.<br/>
        /// 제거할 스타일 속성입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text UnsetStyle<T>(StyleKey<T> property) where T : notnull
        {
            style?.Unset(property);
            return this;
        }

        /// <summary>
        /// Replaces this text's style with a clone of another text's style.<br/>
        /// 이 텍스트의 스타일을 다른 텍스트 스타일의 복제본으로 교체합니다.
        /// </summary>
        /// <param name="target">
        /// The text whose style should be copied.<br/>
        /// 복사할 스타일을 가진 텍스트입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text CopyStyleFrom(Text target)
        {
            style = target.style?.Clone();
            return this;
        }

        /// <summary>
        /// Sets or clears bold styling.<br/>
        /// 굵게 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable bold styling; <see langword="false"/> to disable it.<br/>
        /// 굵게 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Bold(bool value = true) => SetStyle(TextStyles.bold, value);

        /// <summary>
        /// Sets or clears italic styling.<br/>
        /// 기울임 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable italic styling; <see langword="false"/> to disable it.<br/>
        /// 기울임 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Italic(bool value = true) => SetStyle(TextStyles.italic, value);

        /// <summary>
        /// Sets or clears strikethrough styling.<br/>
        /// 취소선 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable strikethrough styling; <see langword="false"/> to disable it.<br/>
        /// 취소선 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Strikethrough(bool value = true) => SetStyle(TextStyles.strikethrough, value);

        /// <summary>
        /// Sets or clears underline styling.<br/>
        /// 밑줄 스타일을 설정하거나 해제합니다.
        /// </summary>
        /// <param name="value">
        /// <see langword="true"/> to enable underline styling; <see langword="false"/> to disable it.<br/>
        /// 밑줄 스타일을 켜려면 <see langword="true"/>, 끄려면 <see langword="false"/>입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Underline(bool value = true) => SetStyle(TextStyles.underline, value);

        /// <summary>
        /// Sets the foreground color style.<br/>
        /// 전경색 스타일을 설정합니다.
        /// </summary>
        /// <param name="value">
        /// The color to assign.<br/>
        /// 할당할 색상입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Color(HexColor value) => SetStyle(TextStyles.color, value);

        /// <summary>
        /// Sets the mark color style.<br/>
        /// 마크 색상 스타일을 설정합니다.
        /// </summary>
        /// <param name="value">
        /// The mark color to assign.<br/>
        /// 할당할 마크 색상입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Mark(HexColor value) => SetStyle(TextStyles.mark, value);

        /// <summary>
        /// Sets the text size style.<br/>
        /// 텍스트 크기 스타일을 설정합니다.
        /// </summary>
        /// <param name="value">
        /// The size value to assign.<br/>
        /// 할당할 크기 값입니다.
        /// </param>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Size(Length value) => SetStyle(TextStyles.size, value);

        /// <summary>
        /// Sets the foreground color to black.<br/>
        /// 전경색을 검은색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Black() => SetStyle(TextStyles.color, HexColor.black);

        /// <summary>
        /// Sets the foreground color to blue.<br/>
        /// 전경색을 파란색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Blue() => SetStyle(TextStyles.color, HexColor.blue);

        /// <summary>
        /// Sets the foreground color to green.<br/>
        /// 전경색을 초록색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Green() => SetStyle(TextStyles.color, HexColor.green);

        /// <summary>
        /// Sets the foreground color to orange.<br/>
        /// 전경색을 주황색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Orange() => SetStyle(TextStyles.color, HexColor.orange);

        /// <summary>
        /// Sets the foreground color to purple.<br/>
        /// 전경색을 보라색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Purple() => SetStyle(TextStyles.color, HexColor.purple);

        /// <summary>
        /// Sets the foreground color to red.<br/>
        /// 전경색을 빨간색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Red() => SetStyle(TextStyles.color, HexColor.red);

        /// <summary>
        /// Sets the foreground color to white.<br/>
        /// 전경색을 흰색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text White() => SetStyle(TextStyles.color, HexColor.white);

        /// <summary>
        /// Sets the foreground color to yellow.<br/>
        /// 전경색을 노란색으로 설정합니다.
        /// </summary>
        /// <returns>
        /// This text instance.<br/>
        /// 이 텍스트 인스턴스를 반환합니다.
        /// </returns>
        public Text Yellow() => SetStyle(TextStyles.color, HexColor.yellow);
    }
}
