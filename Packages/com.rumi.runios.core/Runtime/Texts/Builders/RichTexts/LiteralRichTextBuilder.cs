#nullable enable
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Builds rich-text output by appending the string stored in <see cref="LiteralText"/>.<br/>
    /// <see cref="LiteralText"/>에 저장된 문자열을 추가해 rich text 출력을 만듭니다.
    /// </summary>
    [TextRenderer(typeof(LiteralText))]
    public class LiteralRichTextBuilder : RichTextBuilder
    {
        /// <inheritdoc/>
        protected override void AppendCore(StringBuilder builder, Text text, TextStyleState styleState)
        {
            LiteralText literalText = (LiteralText)text;
            builder.Append(literalText.text);
        }
    }
}
