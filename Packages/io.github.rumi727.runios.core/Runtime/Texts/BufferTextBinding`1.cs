#nullable enable
using RuniOS.Texts.Builders;

namespace RuniOS.Texts
{
    /// <summary>
    /// Rebuilds a text into a buffer with the specified builder and passes the buffer to a setter whenever a change source it depends on changes.<br/>
    /// 텍스트가 의존하는 변경 원인이 바뀔 때마다 지정된 빌더로 텍스트를 버퍼에 다시 빌드하여 버퍼를 setter에 전달합니다.
    /// </summary>
    /// <typeparam name="TBuffer">
    /// The type of the buffer that receives the output.<br/>
    /// 출력을 받을 버퍼의 타입입니다.
    /// </typeparam>
    /// <remarks>
    /// The same buffer is reused by every rebuild, so the setter must copy what it needs instead of keeping the buffer.
    /// <br/><br/>
    /// 모든 다시 빌드가 같은 버퍼를 재사용하므로, setter는 버퍼를 보관하지 말고 필요한 내용을 복사해야 합니다.
    /// </remarks>
    public sealed class BufferTextBinding<TBuffer> : TextBinding
    {
        /// <summary>
        /// Initializes a binding and immediately builds <paramref name="text"/> into <paramref name="buffer"/>.<br/>
        /// 바인딩을 초기화하고 즉시 <paramref name="text"/>를 <paramref name="buffer"/>에 빌드합니다.
        /// </summary>
        /// <param name="text">
        /// The text to build.<br/>
        /// 빌드할 텍스트입니다.
        /// </param>
        /// <param name="builder">
        /// The builder used to build the text.<br/>
        /// 텍스트를 빌드할 빌더입니다.
        /// </param>
        /// <param name="buffer">
        /// The buffer whose content is replaced by each build.<br/>
        /// 빌드할 때마다 내용이 교체되는 버퍼입니다.
        /// </param>
        /// <param name="setter">
        /// The callback that receives the buffer after each build.<br/>
        /// 각 빌드 후 버퍼를 받는 콜백입니다.
        /// </param>
        public BufferTextBinding(Text text, IBufferTextBuilder<TBuffer> builder, TBuffer buffer, Action<TBuffer> setter) : base(text)
        {
            this.builder = builder;
            this.buffer = buffer;
            this.setter = setter;

            Rebuild();
        }

        readonly IBufferTextBuilder<TBuffer> builder;
        readonly TBuffer buffer;
        readonly Action<TBuffer> setter;

        /// <inheritdoc/>
        protected override void Update(ICollection<ITextChangeSource> dependencies)
        {
            builder.BuildTo(text, buffer, dependencies);
            setter.Invoke(buffer);
        }
    }
}
