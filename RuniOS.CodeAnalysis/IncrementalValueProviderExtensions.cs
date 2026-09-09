using Microsoft.CodeAnalysis;

namespace RuniOS.CodeAnalysis;

public static class IncrementalValueProviderExtensions
{
    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(this IncrementalValuesProvider<TSource?> source) => source.Where(x => x != null)!;

    public static IncrementalValuesProvider<TSource> WhereNotNull<TSource>(this IncrementalValuesProvider<TSource?> source) where TSource : struct => source
        .Where(x => x.HasValue)
        .Select((x, _) => x!.Value);
}