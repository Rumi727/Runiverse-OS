#nullable enable
using RuniOS.Formats;
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Builds rich-text output for <see cref="LocalizationText"/> instances.<br/>
    /// <see cref="LocalizationText"/> 인스턴스의 rich text 출력을 만듭니다.
    /// </summary>
    [CustomTextRenderer(typeof(LocalizationText))]
    public class LocalizationRichTextBuilder : RichTextBuilder
    {
        /// <inheritdoc/>
        /// <exception cref="FormatException">
        /// Thrown when a localization format segment refers to a missing argument, or uses alignment or format with a text argument whose builder cannot consume that information.<br/>
        /// 로컬라이징 format 세그먼트가 존재하지 않는 인수를 참조하거나 정렬 및 format 정보를 처리할 수 없는 텍스트 인수에 해당 정보를 사용하는 경우 발생합니다.
        /// </exception>
        protected override void AppendCore(StringBuilder builder, Text text, TextStyleState styleState)
        {
            LocalizationText localizationText = (LocalizationText)text;

            IReadOnlyList<CompositeFormatSegment>? segments = LocalizationUtility.GetFormatSegments(localizationText.identifier, localizationText.languageCode);
            if (segments == null)
            {
                builder.Append(localizationText.identifier);
                return;
            }

            for (int i = 0; i < segments.Count; i++)
            {
                CompositeFormatSegment segment = segments[i];
                if (segment.isLiteral)
                    builder.Append(segment.literal);
                else
                {
                    if (segment.index < 0 || segment.index >= localizationText.args.Count)
                        throw new FormatException($"Format argument index {segment.index} is out of range for translation '{localizationText.identifier}'. Argument count is {localizationText.args.Count}.");

                    Text arg = localizationText.args[segment.index];

                    RichTextBuilder textBuilder = FindBuilder(arg);
                    if (textBuilder is FormattableRichTextBuilder formattableTMPTextRenderer)
                        formattableTMPTextRenderer.Append(builder, arg, segment.alignment, segment.format, styleState);
                    else
                    {
                        if (segment.alignment != 0 || segment.format != null)
                            throw new FormatException($"Format argument {segment.index} in translation '{localizationText.identifier}' uses alignment or format, but '{arg.GetType().Name}' is not formattable.");

                        textBuilder.Append(builder, arg, styleState);
                    }
                }
            }
        }
    }
}
