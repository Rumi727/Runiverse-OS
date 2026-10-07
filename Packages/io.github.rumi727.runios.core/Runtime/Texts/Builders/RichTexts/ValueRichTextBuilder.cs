#nullable enable
using System.Globalization;
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Builds rich-text output for <see cref="ValueText{T}"/> instances, including stored alignment and format information.<br/>
    /// 저장된 정렬 및 format 정보를 포함해 <see cref="ValueText{T}"/> 인스턴스의 rich text 출력을 만듭니다.
    /// </summary>
    /// <remarks>
    /// Stored alignment and format information on <see cref="ValueText{T}"/> overrides information supplied by the caller.<br/>
    /// The format string is applied when the stored value implements <see cref="System.IFormattable"/>.
    /// <br/><br/>
    /// <see cref="ValueText{T}"/>에 저장된 정렬 및 format 정보는 호출자가 제공한 정보를 대체합니다.<br/>
    /// 저장된 값이 <see cref="System.IFormattable"/>을 구현하면 format 문자열을 적용합니다.
    /// </remarks>
    [TextRenderer(typeof(ValueText<>), useForChildren = true)]
    public class ValueRichTextBuilder<T> : FormattableRichTextBuilder
    {
        /// <inheritdoc/>
        protected override void AppendCore(StringBuilder builder, Text text, int alignment, string? format, TextStyleState styleState)
        {
            ValueText<T> valueText = (ValueText<T>)text;
            if (valueText.alignment != null)
                alignment = valueText.alignment.Value;
            if (valueText.format != null)
                format = valueText.format;

            if (format != null && valueText.value is IFormattable formattable)
                AppendAligned(builder, formattable.ToString(format, CultureInfo.InvariantCulture), alignment);
            else
                AppendAligned(builder, valueText.value ?? default, alignment);
        }

        static void AppendAligned<TValue>(StringBuilder builder, TValue value, int alignment)
        {
            int start = builder.Length;
            AppendRaw(builder, value);

            if (alignment == 0)
                return;

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

        static void AppendRaw<TValue>(StringBuilder builder, TValue value)
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
                case char[] v:
                    builder.Append(v);
                    return;
                case StringBuilder v:
                    builder.Append(v);
                    return;
                default:
                    builder.Append(value);
                    return;
            }
        }
    }
}
