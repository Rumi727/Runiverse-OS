#nullable enable
namespace RuniOS.Texts.Builders
{
    /// <summary>
    /// Carries the state shared across nested build calls of a single text build.<br/>
    /// 하나의 텍스트 빌드에서 중첩 빌드 호출 간 공유되는 상태를 전달합니다.
    /// </summary>
    /// <remarks>
    /// This context is independent of any specific builder.<br/>
    /// A builder that supports change tracking reports every text it visits, and every external source its output depends on, through <see cref="AddDependency"/>.
    /// <br/><br/>
    /// 이 컨텍스트는 특정 빌더에 종속되지 않습니다.<br/>
    /// 변경 추적을 지원하는 빌더는 방문하는 모든 텍스트와 출력이 의존하는 모든 외부 원인을 <see cref="AddDependency"/>로 보고합니다.
    /// </remarks>
    /// <param name="styleState">
    /// The style state shared across nested build calls.<br/>
    /// 중첩 빌드 호출 간 공유되는 스타일 상태입니다.
    /// </param>
    /// <param name="dependencies">
    /// The collection that receives the change sources the build depends on, or <see langword="null"/> to skip collection.<br/>
    /// 빌드가 의존하는 변경 원인을 받을 컬렉션이며, <see langword="null"/>이면 수집하지 않습니다.
    /// </param>
    public readonly struct TextBuildContext(TextStyleState styleState, ICollection<ITextChangeSource>? dependencies)
    {
        /// <summary>
        /// Gets the style state shared across nested build calls.<br/>
        /// 중첩 빌드 호출 간 공유되는 스타일 상태를 가져옵니다.
        /// </summary>
        public TextStyleState styleState { get; } = styleState;

        /// <summary>
        /// Reports a change source that the result of this build depends on.<br/>
        /// Does nothing when no collection was supplied or <paramref name="source"/> was already reported.
        /// <br/><br/>
        /// 이 빌드 결과가 의존하는 변경 원인을 보고합니다.<br/>
        /// 수집할 컬렉션이 없거나 <paramref name="source"/>가 이미 보고된 경우 아무 작업도 하지 않습니다.
        /// </summary>
        /// <param name="source">
        /// The change source to report.<br/>
        /// 보고할 변경 원인입니다.
        /// </param>
        public void AddDependency(ITextChangeSource source)
        {
            if (dependencies != null && !dependencies.Contains(source))
                dependencies.Add(source);
        }
    }
}
