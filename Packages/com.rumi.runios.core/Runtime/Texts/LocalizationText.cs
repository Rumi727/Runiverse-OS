#nullable enable
using RuniOS.Resource;

namespace RuniOS.Texts
{
    /// <summary>
    /// Represents a text value that is resolved from a localization identifier and format arguments.<br/>
    /// 로컬라이징 식별자와 format 인수에서 해석되는 텍스트 값을 나타냅니다.
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
    public class LocalizationText(Identifier identifier, IList<Text> args, string? languageCode = null) : Text
    {
        /// <summary>
        /// Gets the localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자를 가져옵니다.
        /// </summary>
        public Identifier identifier { get; set; } = identifier;

        /// <summary>
        /// Gets the text arguments used by the localized format string.<br/>
        /// 로컬라이징된 format 문자열에서 사용할 텍스트 인수를 가져옵니다.
        /// </summary>
        public IList<Text> args { get; } = args;

        /// <summary>
        /// Gets the language code used to resolve this translation.<br/>
        /// 이 번역을 해석할 때 사용하는 언어 코드를 가져옵니다.
        /// </summary>
        public string? languageCode { get; set; } = languageCode;
    }
}
