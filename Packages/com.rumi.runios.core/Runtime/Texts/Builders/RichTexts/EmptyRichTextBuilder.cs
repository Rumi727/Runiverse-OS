#nullable enable
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Builds rich-text output for <see cref="EmptyText"/> instances.<br/>
    /// <see cref="EmptyText"/> 인스턴스의 rich text 출력을 만듭니다.
    /// </summary>
    [CustomTextRenderer(typeof(EmptyText))]
    public sealed class EmptyRichTextBuilder : RichTextBuilder
    {
        /// <inheritdoc/>
        protected override void AppendCore(StringBuilder builder, Text text, TextStyleState styleState) { }
    }
}
