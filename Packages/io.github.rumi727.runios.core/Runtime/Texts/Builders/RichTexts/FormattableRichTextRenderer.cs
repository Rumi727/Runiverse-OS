#nullable enable
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Provides a rich-text renderer base for text types that can consume alignment and format information.<br/>
    /// 정렬 및 format 정보를 처리할 수 있는 텍스트 타입용 rich text 렌더러 기본 클래스를 제공합니다.
    /// </summary>
    public abstract class FormattableRichTextRenderer : RichTextRenderer
    {
        /// <inheritdoc/>
        protected sealed override void AppendCore(StringBuilder builder, Text text, TextBuildContext context) => AppendCore(builder, text, 0, null, context);

        /// <summary>
        /// Appends rich-text output with explicit alignment and format information.<br/>
        /// 명시적 정렬 및 format 정보로 rich text 출력을 추가합니다.
        /// </summary>
        /// <param name="builder">
        /// The destination string builder.<br/>
        /// 대상 문자열 빌더입니다.
        /// </param>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply.<br/>
        /// 적용할 복합 format 정렬 너비입니다.
        /// </param>
        /// <param name="format">
        /// The format string to apply, or <see langword="null"/> to render without a format string.<br/>
        /// 적용할 format 문자열이며, <see langword="null"/>이면 format 문자열 없이 렌더링합니다.
        /// </param>
        /// <param name="context">
        /// The build context shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 빌드 컨텍스트입니다.
        /// </param>
        public void Append(StringBuilder builder, Text text, int alignment, string? format, TextBuildContext context)
        {
            BeginAppend(builder, text, context);
            AppendCore(builder, text, alignment, format, context);
            EndAppend(builder, text, context);
        }

        /// <summary>
        /// Appends rich-text output for the concrete text type that can consume alignment and format information.<br/>
        /// 정렬 및 format 정보를 처리할 수 있는 구체 텍스트 타입의 rich text 출력을 추가합니다.
        /// </summary>
        /// <param name="builder">
        /// The destination string builder.<br/>
        /// 대상 문자열 빌더입니다.
        /// </param>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="alignment">
        /// The composite-format alignment width to apply.<br/>
        /// 적용할 복합 format 정렬 너비입니다.
        /// </param>
        /// <param name="format">
        /// The format string to apply, or <see langword="null"/> to render without a format string.<br/>
        /// 적용할 format 문자열이며, <see langword="null"/>이면 format 문자열 없이 렌더링합니다.
        /// </param>
        /// <param name="context">
        /// The build context shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 빌드 컨텍스트입니다.
        /// </param>
        protected abstract void AppendCore(StringBuilder builder, Text text, int alignment, string? format, TextBuildContext context);
    }
}
