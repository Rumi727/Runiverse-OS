#nullable enable
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Builds rich-text output for <see cref="GroupText"/> instances.<br/>
    /// <see cref="GroupText"/> 인스턴스의 rich text 출력을 만듭니다.
    /// </summary>
    [TextRenderer(typeof(GroupText))]
    public sealed class GroupRichTextBuilder : RichTextBuilder
    {
        /// <inheritdoc/>
        protected override void AppendCore(StringBuilder builder, Text text, TextStyleState styleState)
        {
            GroupText groupText = (GroupText)text;
            for (int i = 0; i < groupText.count; i++)
            {
                Text item = groupText[i];
                FindBuilder(item).Append(builder, item, styleState);
            }
        }
    }
}
