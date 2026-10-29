#nullable enable
namespace RuniOS.Texts
{
    /// <summary>
    /// Represents a source whose change can alter the rendered result of a text.<br/>
    /// 변경되면 텍스트의 렌더링 결과가 달라질 수 있는 원인을 나타냅니다.
    /// </summary>
    public interface ITextChangeSource
    {
        /// <summary>
        /// Occurs when this source changes.<br/>
        /// 이 원인이 변경되면 발생합니다.
        /// </summary>
        event Action? onChanged;
    }
}
