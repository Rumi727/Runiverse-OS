#nullable enable
using System;

namespace RuniOS.Editor.APIBridge.UnityEditor.Search
{
    public class SearchProvider
    {
        public static Type type { get; } = typeof(global::UnityEditor.Search.SearchProvider);

        protected SearchProvider() { }
    }
}
