#nullable enable
using System.Globalization;
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Builds rich-text output for <see cref="LiteralText"/> instances.<br/>
    /// <see cref="LiteralText"/> 인스턴스의 rich text 출력을 만듭니다.
    /// </summary>
    [CustomTextRenderer(typeof(LiteralText))]
    public class LiteralRichTextBuilder : FormattableRichTextBuilder
    {
        /// <inheritdoc/>
        protected override void AppendCore(StringBuilder builder, Text text, int alignment, string? format, TextStyleState styleState)
        {
            LiteralText literalText = (LiteralText)text;
            if (literalText.alignment != null)
                alignment = literalText.alignment.Value;
            if (literalText.format != null)
                format = literalText.format;

            object resultText;
            if (format != null && literalText.value is IFormattable formattable)
                resultText = formattable.ToString(format, CultureInfo.InvariantCulture);
            else
                resultText = literalText.value ?? string.Empty;

            if (alignment != 0)
                AppendAligned(builder, resultText, alignment);
            else
                AppendRaw(builder, resultText);
        }

        static void AppendAligned(StringBuilder builder, object? value, int alignment)
        {
            int start = builder.Length;
            AppendRaw(builder, value);

            int written = builder.Length - start;
            int width = alignment.Abs();

            int padding = width - written;
            if (padding <= 0)
                return;

            if (alignment > 0)
            {
                for (int i = 0; i < padding; i++)
                    builder.Insert(start, ' ');
            }
            else
            {
                for (int i = 0; i < padding; i++)
                    builder.Append(' ');
            }
        }

        static void AppendRaw(StringBuilder builder, object? value)
        {
            switch (value)
            {
                case null:
                    return;
                case bool v:
                    builder.Append(v);
                    return;
                case sbyte v:
                    builder.Append(v);
                    return;
                case byte v:
                    builder.Append(v);
                    return;
                case short v:
                    builder.Append(v);
                    return;
                case ushort v:
                    builder.Append(v);
                    return;
                case int v:
                    builder.Append(v);
                    return;
                case uint v:
                    builder.Append(v);
                    return;
                case long v:
                    builder.Append(v);
                    return;
                case ulong v:
                    builder.Append(v);
                    return;
                case float v:
                    builder.Append(v);
                    return;
                case double v:
                    builder.Append(v);
                    return;
                case decimal v:
                    builder.Append(v);
                    return;
                case char v:
                    builder.Append(v);
                    return;
                case string v:
                    builder.Append(v);
                    return;
                default:
                    builder.Append(value);
                    return;
            }
        }
    }
}
