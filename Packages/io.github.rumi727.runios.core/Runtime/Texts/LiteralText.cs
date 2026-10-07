#nullable enable
namespace RuniOS.Texts
{
    /// <summary>
    /// Represents text backed by a literal string.<br/>
    /// 리터럴 문자열을 기반으로 하는 텍스트를 나타냅니다.
    /// </summary>
    /// <param name="text">
    /// The literal string represented by this text.<br/>
    /// 이 텍스트가 나타내는 리터럴 문자열입니다.
    /// </param>
    public class LiteralText(string text) : Text
    {
        /// <summary>
        /// Initializes an empty literal string text.<br/>
        /// 빈 리터럴 문자열 텍스트를 초기화합니다.
        /// </summary>
        public LiteralText() : this(string.Empty) { }

        /// <summary>
        /// Gets or sets the literal string represented by this text.<br/>
        /// 이 텍스트가 나타내는 리터럴 문자열을 가져오거나 설정합니다.
        /// </summary>
        public string text { get; set; } = text;
    }
}
