#nullable enable
namespace RuniOS.IO
{
    /// <summary>
    /// Matches the paths that the specified <see cref="matcher"/> does not match.<br/>
    /// 지정한 <see cref="matcher"/>가 일치하지 않는 경로와 일치합니다.
    /// </summary>
    public readonly struct NotPatternMatcher(IPatternMatcher matcher) : IPatternMatcher
    {
        /// <summary>
        /// Gets the matcher whose result is inverted.<br/>
        /// 결과를 뒤집을 매처를 가져옵니다.
        /// </summary>
        public IPatternMatcher matcher { get; } = matcher;

        public bool IsMatch(scoped ReadOnlySpan<char> path) => !matcher.IsMatch(path);
    }
}
