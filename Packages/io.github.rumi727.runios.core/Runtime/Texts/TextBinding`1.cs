#nullable enable
using RuniOS.Texts.Builders;

namespace RuniOS.Texts
{
    /// <summary>
    /// Rebuilds a text with the specified builder and passes the result to a setter whenever a change source it depends on changes.<br/>
    /// 텍스트가 의존하는 변경 원인이 바뀔 때마다 지정된 빌더로 텍스트를 다시 빌드하여 결과를 setter에 전달합니다.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the build result.<br/>
    /// 빌드 결과의 타입입니다.
    /// </typeparam>
    public sealed class TextBinding<T> : TextBinding
    {
        /// <summary>
        /// Initializes a binding and immediately builds <paramref name="text"/> into <paramref name="setter"/>.<br/>
        /// 바인딩을 초기화하고 즉시 <paramref name="text"/>를 빌드하여 <paramref name="setter"/>에 전달합니다.
        /// </summary>
        /// <param name="text">
        /// The text to build.<br/>
        /// 빌드할 텍스트입니다.
        /// </param>
        /// <param name="builder">
        /// The builder used to build the text.<br/>
        /// 텍스트를 빌드할 빌더입니다.
        /// </param>
        /// <param name="setter">
        /// The callback that receives each build result.<br/>
        /// 각 빌드 결과를 받는 콜백입니다.
        /// </param>
        public TextBinding(Text text, ITextBuilder<T> builder, Action<T> setter) : base(text)
        {
            this.builder = builder;
            this.setter = setter;

            Rebuild();
        }

        readonly ITextBuilder<T> builder;
        readonly Action<T> setter;

        /// <inheritdoc/>
        protected override void Update(ICollection<ITextChangeSource> dependencies) => setter.Invoke(builder.Build(text, dependencies));
    }
}
