#nullable enable
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Provides a rich-text builder base for text types that can consume alignment and format information.<br/>
    /// 정렬 및 format 정보를 사용할 수 있는 텍스트 타입용 rich text 빌더 기본 클래스를 제공합니다.
    /// </summary>
    public abstract class FormattableRichTextBuilder : RichTextBuilder
    {
        protected sealed override void AppendCore(StringBuilder builder, Text text, TextStyleState styleState) => AppendCore(builder, text, 0, null, styleState);

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
        /// <param name="styleState">
        /// The style state shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 스타일 상태입니다.
        /// </param>
        public void Append(StringBuilder builder, Text text, int alignment, string? format, TextStyleState styleState) => AppendCore(builder, text, alignment, format, styleState);
        /// <summary>
        /// Appends rich-text output for the concrete formattable text type.<br/>
        /// 구체 format 가능 텍스트 타입의 rich text 출력을 추가합니다.
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
        /// <param name="styleState">
        /// The style state shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 스타일 상태입니다.
        /// </param>
        protected abstract void AppendCore(StringBuilder builder, Text text, int alignment, string? format, TextStyleState styleState);
    }
}
