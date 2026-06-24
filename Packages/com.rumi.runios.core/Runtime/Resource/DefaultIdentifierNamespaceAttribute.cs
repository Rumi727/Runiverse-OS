#nullable enable

namespace RuniOS.Resource
{
    /// <summary>
    /// Defines the default <see cref="Identifier"/> namespace for an assembly.<br/>
    /// 어셈블리의 기본 <see cref="Identifier"/> 네임스페이스를 정의합니다.
    /// </summary>
    /// <param name="nameSpace">
    /// The namespace used by caller-aware identifier creation when no namespace is explicitly supplied.<br/>
    /// 명시적인 네임스페이스가 없을 때 호출자 인식 식별자 생성에서 사용할 네임스페이스입니다.
    /// </param>
    /// <remarks>
    /// This attribute lets a game, package, or module own its shorthand identifiers without changing the framework fallback namespace.<br/>
    /// Code paths that cannot identify a meaningful caller should pass an explicit fallback namespace instead.
    /// <br/><br/>
    /// 이 특성은 프레임워크 폴백 네임스페이스를 바꾸지 않고도 게임, 패키지, 모듈이 자신의 축약 식별자를 소유하게 합니다.<br/>
    /// 의미 있는 호출자를 식별할 수 없는 코드 경로는 대신 명시적인 폴백 네임스페이스를 전달해야 합니다.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class DefaultIdentifierNamespaceAttribute(string nameSpace) : Attribute
    {
        /// <summary>
        /// Gets the namespace assigned to the assembly.<br/>
        /// 어셈블리에 할당된 네임스페이스를 가져옵니다.
        /// </summary>
        public string nameSpace { get; } = nameSpace;
    }
}