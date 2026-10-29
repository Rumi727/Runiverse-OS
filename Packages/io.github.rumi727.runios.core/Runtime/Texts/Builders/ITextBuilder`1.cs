#nullable enable
namespace RuniOS.Texts.Builders
{
    /// <summary>
    /// Defines a text builder that converts a whole <see cref="Text"/> tree to a <typeparamref name="TResult"/> output.<br/>
    /// A builder reports every change source its output depends on, so the output can be kept up to date by <see cref="TextBinding{T}"/>.
    /// <br/><br/>
    /// <see cref="Text"/> 트리 전체를 <typeparamref name="TResult"/> 출력으로 변환하는 텍스트 빌더를 정의합니다.<br/>
    /// 빌더는 출력이 의존하는 모든 변경 원인을 보고하므로, <see cref="TextBinding{T}"/>로 출력을 최신 상태로 유지할 수 있습니다.
    /// </summary>
    /// <typeparam name="TResult">
    /// The type of the build output.<br/>
    /// 빌드 출력의 타입입니다.
    /// </typeparam>
    public interface ITextBuilder<out TResult>
    {
        /// <summary>
        /// Builds the output of a text.<br/>
        /// 텍스트의 출력을 빌드합니다.
        /// </summary>
        /// <param name="text">
        /// The text to build.<br/>
        /// 빌드할 텍스트입니다.
        /// </param>
        /// <param name="dependencies">
        /// The collection that receives the change sources the output depends on, or <see langword="null"/> to skip collection.<br/>
        /// 출력이 의존하는 변경 원인을 받을 컬렉션이며, <see langword="null"/>이면 수집하지 않습니다.
        /// </param>
        /// <returns>
        /// The built output.<br/>
        /// 빌드된 출력을 반환합니다.
        /// </returns>
        TResult Build(Text text, ICollection<ITextChangeSource>? dependencies = null);
    }
}
