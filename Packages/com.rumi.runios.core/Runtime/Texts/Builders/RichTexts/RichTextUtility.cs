#nullable enable
using RuniOS.Texts.Styles;
using RuniOS.Texts.Styles.TMPro;
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    static class RichTextUtility
    {
        public static void OpenStyle(StringBuilder builder, TextStyleState styleState)
        {
            OpenRich(TextStyles.bold, builder, styleState);
            OpenRich(TextStyles.color, builder, styleState);
            OpenRich(TextStyles.italic, builder, styleState);
            OpenRich(TextStyles.mark, builder, styleState);
            OpenRich(TextStyles.size, builder, styleState);
            OpenRich(TextStyles.strikethrough, builder, styleState);
            OpenRich(TextStyles.underline, builder, styleState);

            OpenRich(TMPStyles.align, (x, builder) => builder.Append(x switch
            {
                TextAlign.Left => "left",
                TextAlign.Center => "center",
                TextAlign.Right => "right",
                TextAlign.Justified => "justified",
                TextAlign.Flush => "flush",
                _ => throw new ArgumentOutOfRangeException(nameof(x), x, null)
            }), builder, styleState);

            OpenRich(TMPStyles.alpha, (x, builder) => builder.AppendFormat("{0:X2}", (x * 255).RoundToInt()), builder, styleState);
            OpenRich(TMPStyles.characterSpacing, builder, styleState);
            OpenRich(TMPStyles.font, builder, styleState);
            OpenRich(TMPStyles.fontWeight, (x, builder) => builder.Append('"').Append(x).Append('"'), builder, styleState);
            OpenRich(TMPStyles.gradient, builder, styleState);
            OpenRich(TMPStyles.indent, builder, styleState);
            OpenRich(TMPStyles.lineHeight, builder, styleState);
            OpenRich(TMPStyles.lineIndent, builder, styleState);
            OpenRich(TMPStyles.lowercase, builder, styleState);
            OpenRich(TMPStyles.margin, builder, styleState);
            OpenRich(TMPStyles.monoSpacing, builder, styleState);
            OpenRich(TMPStyles.noBreak, builder, styleState);
            OpenRich(TMPStyles.position, builder, styleState);
            OpenRich(TMPStyles.rotation, builder, styleState);
            OpenRich(TMPStyles.smallcaps, builder, styleState);
            OpenRich(TMPStyles.subscript, builder, styleState);
            OpenRich(TMPStyles.superscript, builder, styleState);
            OpenRich(TMPStyles.uppercase, builder, styleState);
            OpenRich(TMPStyles.verticalOffset, builder, styleState);
            OpenRich(TMPStyles.width, builder, styleState);
        }

        public static void CloseStyle(StringBuilder builder, TextStyleState styleState)
        {
            CloseRich(TMPStyles.width, builder, styleState);
            CloseRich(TMPStyles.verticalOffset, builder, styleState);
            CloseRich(TMPStyles.uppercase, builder, styleState);
            CloseRich(TMPStyles.superscript, builder, styleState);
            CloseRich(TMPStyles.subscript, builder, styleState);
            CloseRich(TMPStyles.smallcaps, builder, styleState);
            CloseRich(TMPStyles.rotation, builder, styleState);
            CloseRich(TMPStyles.position, builder, styleState);
            CloseRich(TMPStyles.noBreak, builder, styleState);
            CloseRich(TMPStyles.monoSpacing, builder, styleState);
            CloseRich(TMPStyles.margin, builder, styleState);
            CloseRich(TMPStyles.lowercase, builder, styleState);
            CloseRich(TMPStyles.lineIndent, builder, styleState);
            CloseRich(TMPStyles.lineHeight, builder, styleState);
            CloseRich(TMPStyles.indent, builder, styleState);
            CloseRich(TMPStyles.gradient, builder, styleState);
            CloseRich(TMPStyles.fontWeight, builder, styleState);
            CloseRich(TMPStyles.font, builder, styleState);
            CloseRich(TMPStyles.characterSpacing, builder, styleState);
            CloseRich(TMPStyles.alpha, builder, styleState);

            CloseRich(TMPStyles.align, builder, styleState);

            CloseRich(TextStyles.underline, builder, styleState);
            CloseRich(TextStyles.strikethrough, builder, styleState);
            CloseRich(TextStyles.size, builder, styleState);
            CloseRich(TextStyles.mark, builder, styleState);
            CloseRich(TextStyles.italic, builder, styleState);
            CloseRich(TextStyles.color, builder, styleState);
            CloseRich(TextStyles.bold, builder, styleState);
        }

        static void OpenRich(StyleKey<bool> key, StringBuilder builder, TextStyleState styleState)
        {
            StyleProperty<bool> property = styleState.current.Get(key);
            if (!property.hasValue)
                return;

            StyleProperty<bool> parentProperty = styleState.GetParent(key);
            if (parentProperty.hasValue && parentProperty.value)
            {
                if (property.value)
                    return;
                else
                {
                    // 오버라이드됨
                    builder.Append("</").Append(key).Append(">");
                }
            }
            else if (property.value)
                builder.Append("<").Append(key).Append(">");
        }

        static void OpenRich(StyleKey<string> key, StringBuilder builder, TextStyleState styleState)
        {
            StyleProperty<string> property = styleState.current.Get(key);
            if (!property.hasValue)
                return;

            builder.Append("<").Append(key).Append("=").Append('"');
            for (int i = 0; i < property.value.Length; i++)
            {
                char c = property.value[i];
                switch (c)
                {
                    case '\\':
                    {
                        builder.Append(@"\\");
                        break;
                    }
                    case '"':
                    {
                        builder.Append("\\\"");
                        break;
                    }
                    default:
                    {
                        builder.Append(c);
                        break;
                    }
                }
            }
            builder.Append('"').Append(">");
        }

        static void OpenRich<T>(StyleKey<T> key, StringBuilder builder, TextStyleState styleState)
        {
            StyleProperty<T> property = styleState.current.Get(key);
            if (!property.hasValue)
                return;

            builder.Append("<").Append(key).Append("=");
            if (property.value is string stringValue)
            {
                builder.Append('"');
                if (stringValue.Contains('\"'))
                    builder.Append(stringValue.Replace("\"", "\\\""));
                else
                    builder.Append(stringValue);
                builder.Append('"');
            }
            else
                builder.Append(property.value);
            builder.Append(">");
        }

        static void OpenRich<T>(StyleKey<T> key, Action<T, StringBuilder> optionAction, StringBuilder builder, TextStyleState styleState)
        {
            StyleProperty<T> property = styleState.current.Get(key);
            if (!property.hasValue)
                return;

            builder.Append("<").Append(key).Append("=");
            optionAction.Invoke(property.value, builder);
            builder.Append(">");
        }

        static void CloseRich(StyleKey<bool> key, StringBuilder builder, TextStyleState styleState)
        {
            StyleProperty<bool> property = styleState.current.Get(key);
            if (!property.hasValue)
                return;

            StyleProperty<bool> parentProperty = styleState.GetParent(key);
            if (parentProperty.hasValue && parentProperty.value)
            {
                if (property.value)
                    return;
                else
                {
                    // 오버라이드됨
                    builder.Append("<").Append(key).Append(">");
                }
            }
            else if (property.value)
                builder.Append("</").Append(key).Append(">");
        }

        static void CloseRich<T>(StyleKey<T> key, StringBuilder builder, TextStyleState styleState)
        {
            StyleProperty<T> property = styleState.current.Get(key);
            if (!property.hasValue)
                return;

            builder.Append("</").Append(key).Append(">");
        }
    }
}
