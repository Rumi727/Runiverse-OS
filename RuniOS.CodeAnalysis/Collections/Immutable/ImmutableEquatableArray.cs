using System.Collections.Generic;
using System.Collections.Immutable;

namespace RuniOS.CodeAnalysis.Collections.Immutable;

public static class ImmutableEquatableArray
{
    public static ImmutableEquatableArray<TSource> ToImmutableEquatableArray<TSource>(this IEnumerable<TSource> items)
    {
        return items switch
        {
            ImmutableEquatableArray<TSource> immutableEquatableArray => immutableEquatableArray,
            ImmutableArray<TSource> immutableArray => new ImmutableEquatableArray<TSource>(immutableArray),
            _ => new ImmutableEquatableArray<TSource>(ImmutableArray.CreateRange(items))
        };
    }
}