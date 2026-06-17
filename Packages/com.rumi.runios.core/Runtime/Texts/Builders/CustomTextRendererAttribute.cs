#nullable enable
namespace RuniOS.Texts.Builders
{
    /// <summary>
    /// Marks a text builder as the renderer for a specific <see cref="Text"/> type.<br/>
    /// 텍스트 빌더를 특정 <see cref="Text"/> 타입의 렌더러로 표시합니다.
    /// </summary>
    /// <param name="targetType">
    /// The text type handled by the annotated builder.<br/>
    /// 특성이 지정된 빌더가 처리할 텍스트 타입입니다.
    /// </param>
    /// <param name="isSubtypeCompatible">
    /// Whether the builder can handle subclasses of <paramref name="targetType"/>.<br/>
    /// 빌더가 <paramref name="targetType"/>의 하위 클래스를 처리할 수 있는지 여부입니다.
    /// </param>
    public class CustomTextRendererAttribute(Type targetType, bool isSubtypeCompatible = false) : TypeHandlerAttribute(targetType)
    {
        /// <inheritdoc/>
        public override bool isSubtypeCompatible { get; } = isSubtypeCompatible;
    }
}
