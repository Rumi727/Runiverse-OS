using System.Collections.Generic;
using System.Linq;

namespace RuniOS.CodeAnalysis.Linq;

static class LinqExtra
{
    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source) => source.Where(x => x != null)!;

    public static IEnumerable<TSource> WhereNotNull<TSource>(this IEnumerable<TSource?> source) where TSource : struct => source
        .Where(x => x.HasValue)
        .Select(x => x!.Value);
}