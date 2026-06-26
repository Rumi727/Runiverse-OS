#nullable enable
namespace RuniOS.Texts
{
    /// <summary>
    /// Represents text backed by a value that can use composite-format alignment and format information.<br/>
    /// 값을 기반으로 하며 복합 format 정렬 및 format 정보를 사용할 수 있는 텍스트를 나타냅니다.
    /// </summary>
    /// <param name="value">
    /// The value represented by this text.<br/>
    /// 이 텍스트가 나타내는 값입니다.
    /// </param>
    /// <param name="alignment">
    /// The composite-format alignment width stored by this text, or <see langword="null"/> to use the caller-provided alignment.<br/>
    /// 이 텍스트가 저장하는 복합 format 정렬 너비이며, <see langword="null"/>이면 호출자가 제공한 정렬 값을 사용합니다.
    /// </param>
    /// <param name="format">
    /// The format string stored by this text, or <see langword="null"/> to use the caller-provided format.<br/>
    /// 이 텍스트가 저장하는 format 문자열이며, <see langword="null"/>이면 호출자가 제공한 format을 사용합니다.
    /// </param>
    public class ValueText<T>(T value, int? alignment, string? format) : Text
    {
        /// <summary>
        /// Initializes a <see cref="ValueText{T}"/> instance without stored alignment or format information.<br/>
        /// 저장된 정렬 또는 format 정보가 없는 <see cref="ValueText{T}"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값입니다.
        /// </param>
        public ValueText(T value) : this(value, null, null) { }

        /// <summary>
        /// Initializes a <see cref="ValueText{T}"/> instance with alignment information.<br/>
        /// 정렬 정보를 가진 <see cref="ValueText{T}"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply when formatting.<br/>
        /// format 처리 시 적용할 복합 format 정렬 너비입니다.
        /// </param>
        public ValueText(T value, int alignment) : this(value, alignment, null) { }

        /// <summary>
        /// Initializes a <see cref="ValueText{T}"/> instance with format information.<br/>
        /// format 정보를 가진 <see cref="ValueText{T}"/> 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="value">
        /// The value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값입니다.
        /// </param>
        /// <param name="format">
        /// The format string used when <paramref name="value"/> implements <see cref="System.IFormattable"/>.<br/>
        /// <paramref name="value"/>가 <see cref="System.IFormattable"/>을 구현할 때 사용할 format 문자열입니다.
        /// </param>
        public ValueText(T value, string format) : this(value, null, format) { }

        /// <summary>
        /// Gets the value represented by this text.<br/>
        /// 이 텍스트가 나타내는 값을 가져옵니다.
        /// </summary>
        public T value { get; set; } = value;

        /// <summary>
        /// Gets or sets the stored composite-format alignment width.<br/>
        /// 저장된 복합 format 정렬 너비를 가져오거나 설정합니다.
        /// </summary>
        public int? alignment { get; set; } = alignment;

        /// <summary>
        /// Gets or sets the stored format string used when the value implements <see cref="System.IFormattable"/>.<br/>
        /// 값이 <see cref="System.IFormattable"/>을 구현할 때 사용할 저장된 format 문자열을 가져오거나 설정합니다.
        /// </summary>
        public string? format { get; set; } = format;
    }
}
