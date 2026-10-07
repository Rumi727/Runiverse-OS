#nullable enable
namespace RuniOS.IO
{
    public struct AllPatternMatcher : IPatternMatcher
    {
        public bool IsMatch(scoped ReadOnlySpan<char> path) => true;
    }
}