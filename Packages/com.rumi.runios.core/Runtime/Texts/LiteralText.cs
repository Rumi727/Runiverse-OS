#nullable enable
namespace RuniOS.Texts
{
    /// <summary>
    /// Represents text backed by a literal or formattable value.<br/>
    /// 리터럴 또는 포맷 가능한 값을 기반으로 하는 텍스트를 나타냅니다.
    /// </summary>
    /// <param name="value">
    /// The value represented by this text.<br/>
    /// 이 텍스트가 나타내는 값입니다.
    /// </param>
    /// <param name="alignment">
    /// The composite-format alignment width to apply when formatting, or <see langword="null"/> to use the caller-provided alignment.<br/>
    /// format 처리 시 적용할 복합 format 정렬 너비이며, <see langword="null"/>이면 호출자가 제공한 정렬 값을 사용합니다.
    /// </param>
    /// <param name="format">
    /// The format string to apply when formatting, or <see langword="null"/> to use the caller-provided format.<br/>
    /// format 처리 시 적용할 format 문자열이며, <see langword="null"/>이면 호출자가 제공한 format을 사용합니다.
    /// </param>
    public class LiteralText(object? value, int? alignment, string? format) : Text
    {
        /// <summary>
        /// Initializes a literal text value.<br/>
        /// 리터럴 텍스트 값을 초기화합니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값입니다.
        /// </param>
        public LiteralText(object? value) : this(value, null, null) { }

        /// <summary>
        /// Initializes a literal text value with alignment information.<br/>
        /// 정렬 정보를 가진 리터럴 텍스트 값을 초기화합니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply when formatting.<br/>
        /// format 처리 시 적용할 복합 format 정렬 너비입니다.
        /// </param>
        public LiteralText(object? value, int alignment) : this(value, alignment, null) { }

        /// <summary>
        /// Initializes a literal text value with format information.<br/>
        /// format 정보를 가진 리터럴 텍스트 값을 초기화합니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값입니다.
        /// </param>
        /// <param name="format">
        /// The format string used when <paramref name="value"/> implements <see cref="System.IFormattable"/>.<br/>
        /// <paramref name="value"/>가 <see cref="System.IFormattable"/>을 구현할 때 사용할 format 문자열입니다.
        /// </param>
        public LiteralText(object? value, string format) : this(value, null, format) { }

        /// <summary>
        /// Gets the value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값을 가져옵니다.
        /// </summary>
        public object? value { get; set; } = value;

        /// <summary>
        /// Gets or sets the composite-format alignment width, if one is specified.<br/>
        /// 지정된 경우 복합 format 정렬 너비를 가져오거나 설정합니다.
        /// </summary>
        public int? alignment { get; set; } = alignment;

        /// <summary>
        /// Gets or sets the format string used when the value is formattable, if one is specified.<br/>
        /// 지정된 경우 값이 format 가능할 때 사용할 format 문자열을 가져오거나 설정합니다.
        /// </summary>
        public string? format { get; set; } = format;
    }
}
