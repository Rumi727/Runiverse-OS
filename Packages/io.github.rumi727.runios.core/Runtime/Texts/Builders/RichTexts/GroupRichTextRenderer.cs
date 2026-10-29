#nullable enable
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Builds rich-text output for <see cref="GroupText"/> instances.<br/>
    /// <see cref="GroupText"/> 인스턴스의 rich text 출력을 만듭니다.
    /// </summary>
    [TextRenderer(typeof(GroupText))]
    public sealed class GroupRichTextRenderer : RichTextRenderer
    {
        /// <inheritdoc/>
        protected override void AppendCore(StringBuilder builder, Text text, TextBuildContext context)
        {
            GroupText groupText = (GroupText)text;
            for (int i = 0; i < groupText.count; i++)
            {
                Text item = groupText[i];
                FindRenderer(item).Append(builder, item, context);
            }
        }
    }
}
