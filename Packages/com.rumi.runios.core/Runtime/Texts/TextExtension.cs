#nullable enable
using RuniOS.Texts.Builders.RichTexts;
using System.Text;

namespace RuniOS.Texts
{
    public static class TextExtension
    {
        public static string ToRichText(this Text text) => RichTextBuilder.Build(text);
        public static void BuildTo(this Text text, StringBuilder builder) => RichTextBuilder.BuildTo(text, builder);
    }
}