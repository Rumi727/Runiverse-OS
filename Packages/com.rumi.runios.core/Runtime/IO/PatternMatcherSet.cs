#nullable enable
namespace RuniOS.IO
{
    public static class PatternMatcherSet
    {
        public static PatternMatcherSet<T> Create<T>() where T : IPatternMatcher => PatternMatcherSet<T>.empty;
        public static PatternMatcherSet<T> Create<T>(IEnumerable<T> matchers) where T : IPatternMatcher => new PatternMatcherSet<T>(matchers);
        public static PatternMatcherSet<T> Create<T>(params ReadOnlySpan<T> matchers) where T : IPatternMatcher
        {
            if (matchers.IsEmpty)
                return PatternMatcherSet<T>.empty;

            return new PatternMatcherSet<T>(matchers);
        }

        public static PatternMatcherSet<T> Create<T>(params ReadOnlySpan<PatternMatcherSet<T>> matcherSets) where T : IPatternMatcher
        {
            if (matcherSets.IsEmpty)
                return PatternMatcherSet<T>.empty;

            if (matcherSets.Length == 1)
                return matcherSets[0];

            int count = 0;
            foreach (var set in matcherSets)
                count += set.matchers.Length;

            T[] matchers = new T[count];
            int offset = 0;

            foreach (var set in matcherSets)
            {
                set.matchers.CopyTo(matchers, offset);
                offset += set.matchers.Length;
            }

            return new PatternMatcherSet<T>(matchers);
        }

        public static PatternMatcherSet<ExtensionMatcher> CreateExt(params IEnumerable<string> extensions) => CreateExt(extensions.Select(x => (FileExtension)x));
        public static PatternMatcherSet<ExtensionMatcher> CreateExt(params IEnumerable<FileExtension> extensions) => new PatternMatcherSet<ExtensionMatcher>(extensions.Select(x => new ExtensionMatcher(x)));
    }
}