#nullable enable
using System.Collections;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace RuniOS.IO
{
    [CollectionBuilder(typeof(PatternMatcherSet), "Create")]
    public sealed class PatternMatcherSet<T> : IPatternMatcher, IEnumerable<T> where T : IPatternMatcher
    {
        // ReSharper disable once UseCollectionExpression
        public static PatternMatcherSet<T> empty { get; } = new PatternMatcherSet<T>();

        public PatternMatcherSet() { }
        public PatternMatcherSet(IEnumerable<T> matchers) => this.matchers = [..matchers];
        public PatternMatcherSet(params ReadOnlySpan<T> matchers) => this.matchers = [..matchers];

        public ImmutableArray<T> matchers { get; } = [];

        public bool IsMatch(scoped ReadOnlySpan<char> path)
        {
            if (matchers.IsEmpty)
                return false;

            foreach (var matcher in matchers)
            {
                if (matcher.IsMatch(path))
                    return true;
            }

            return false;
        }

        public ImmutableArray<T>.Enumerator GetEnumerator() => matchers.GetEnumerator();
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)matchers).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)matchers).GetEnumerator();
    }
}