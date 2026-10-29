#nullable enable
namespace RuniOS.Texts.Builders
{
    /// <summary>
    /// Defines a text builder that writes the output of a whole <see cref="Text"/> tree into a <typeparamref name="TBuffer"/> buffer.<br/>
    /// A builder reports every change source its output depends on, so the buffer can be kept up to date by <see cref="BufferTextBinding{TBuffer}"/>.
    /// <br/><br/>
    /// <see cref="Text"/> 트리 전체의 출력을 <typeparamref name="TBuffer"/> 버퍼에 쓰는 텍스트 빌더를 정의합니다.<br/>
    /// 빌더는 출력이 의존하는 모든 변경 원인을 보고하므로, <see cref="BufferTextBinding{TBuffer}"/>로 버퍼를 최신 상태로 유지할 수 있습니다.
    /// </summary>
    /// <typeparam name="TBuffer">
    /// The type of the buffer that receives the output.<br/>
    /// 출력을 받을 버퍼의 타입입니다.
    /// </typeparam>
    public interface IBufferTextBuilder<TBuffer>
    {
        /// <summary>
        /// Replaces the content of a buffer with the output of a text.<br/>
        /// 버퍼의 내용을 텍스트의 출력으로 교체합니다.
        /// </summary>
        /// <param name="text">
        /// The text to build.<br/>
        /// 빌드할 텍스트입니다.
        /// </param>
        /// <param name="buffer">
        /// The buffer whose content is replaced.<br/>
        /// 내용이 교체될 버퍼입니다.
        /// </param>
        /// <param name="dependencies">
        /// The collection that receives the change sources the output depends on, or <see langword="null"/> to skip collection.<br/>
        /// 출력이 의존하는 변경 원인을 받을 컬렉션이며, <see langword="null"/>이면 수집하지 않습니다.
        /// </param>
        void BuildTo(Text text, TBuffer buffer, ICollection<ITextChangeSource>? dependencies = null);
    }
}
