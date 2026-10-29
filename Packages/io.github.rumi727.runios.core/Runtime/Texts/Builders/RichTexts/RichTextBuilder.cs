#nullable enable
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Converts <see cref="Text"/> trees into rich-text strings by using the registered <see cref="RichTextRenderer"/> types.<br/>
    /// 등록된 <see cref="RichTextRenderer"/> 타입을 사용하여 <see cref="Text"/> 트리를 rich text 문자열로 변환합니다.
    /// </summary>
    /// <remarks>
    /// <b>This builder is stateless.</b><br/>
    /// <see cref="instance"/> is shared by every caller, and per-build state is kept in <see cref="TextBuildContext"/>.<br/>
    /// Because <see cref="Text"/> is mutable and not thread-safe, builds are not designed to run concurrently on multiple threads.
    /// <br/><br/>
    /// <b>이 빌더는 상태를 가지지 않습니다.</b><br/>
    /// <see cref="instance"/>는 모든 호출자가 공유하며, 빌드별 상태는 <see cref="TextBuildContext"/>에 보관됩니다.<br/>
    /// <see cref="Text"/>는 뮤터블하고 thread-safe하지 않으므로, 여러 스레드에서 동시에 빌드하는 것을 전제로 설계되지 않았습니다.
    /// </remarks>
    public sealed class RichTextBuilder : ITextBuilder<string>, IBufferTextBuilder<StringBuilder>
    {
        RichTextBuilder() { }

        public static RichTextBuilder instance { get; } = new RichTextBuilder();

        /// <summary>
        /// Builds a rich-text string from a text instance.<br/>
        /// 텍스트 인스턴스에서 rich text 문자열을 만듭니다.
        /// </summary>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="dependencies">
        /// The collection that receives the change sources the result depends on, or <see langword="null"/> to skip collection.<br/>
        /// 결과가 의존하는 변경 원인을 받을 컬렉션이며, <see langword="null"/>이면 수집하지 않습니다.
        /// </param>
        /// <returns>
        /// The rendered rich-text string.<br/>
        /// 렌더링된 rich text 문자열을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="text"/> is <see langword="null"/>.<br/>
        /// <paramref name="text"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no rich-text renderer is registered for a text type in the tree.<br/>
        /// 트리 안의 텍스트 타입에 등록된 rich text 렌더러가 없는 경우 발생합니다.
        /// </exception>
        public static string Build(Text text, ICollection<ITextChangeSource>? dependencies = null)
        {
            ExceptionUtility.ThrowIfArgumentNull(text);

            StringBuilder builder = StringBuilderCache.Acquire();
            try
            {
                BuildTo(text, builder, dependencies);
                return builder.ToString();
            }
            finally
            {
                StringBuilderCache.Release(builder);
            }
        }

        /// <summary>
        /// Replaces the content of a string builder with the rich-text output of a text instance.<br/>
        /// 문자열 빌더의 내용을 텍스트 인스턴스의 rich text 출력으로 교체합니다.
        /// </summary>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="builder">
        /// The string builder whose content is replaced.<br/>
        /// 내용이 교체될 문자열 빌더입니다.
        /// </param>
        /// <param name="dependencies">
        /// The collection that receives the change sources the result depends on, or <see langword="null"/> to skip collection.<br/>
        /// 결과가 의존하는 변경 원인을 받을 컬렉션이며, <see langword="null"/>이면 수집하지 않습니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="text"/> is <see langword="null"/>.<br/>
        /// <paramref name="text"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no rich-text renderer is registered for a text type in the tree.<br/>
        /// 트리 안의 텍스트 타입에 등록된 rich text 렌더러가 없는 경우 발생합니다.
        /// </exception>
        public static void BuildTo(Text text, StringBuilder builder, ICollection<ITextChangeSource>? dependencies = null)
        {
            builder.Clear();

            TextStyleState styleState = TextStyleStateCache.Acquire();
            try
            {
                RichTextRenderer.FindRenderer(text).Append(builder, text, new TextBuildContext(styleState, dependencies));
            }
            finally
            {
                TextStyleStateCache.Release(styleState);
            }
        }

        string ITextBuilder<string>.Build(Text text, ICollection<ITextChangeSource>? dependencies) => Build(text, dependencies);
        void IBufferTextBuilder<StringBuilder>.BuildTo(Text text, StringBuilder buffer, ICollection<ITextChangeSource>? dependencies) => BuildTo(text, buffer, dependencies);
    }
}
