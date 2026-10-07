#nullable enable
using System.Text.RegularExpressions;

namespace RuniOS.IO
{
    public sealed class RegexMatcher(Regex regex) : IPatternMatcher
    {
        public Regex? regex { get; } = regex;

        public bool IsMatch(scoped ReadOnlySpan<char> path) => regex?.IsMatch(path.ToString()) ?? false;
    }
}