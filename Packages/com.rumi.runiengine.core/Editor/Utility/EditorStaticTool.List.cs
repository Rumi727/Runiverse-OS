#nullable enable
using System.Collections;
using System.Collections.Generic;

namespace RuniEngine.Editor
{
    public static partial class EditorStaticTool
    {
        public static IEnumerable<DictionaryEntry> ToGeneric(this IDictionary source)
        {
            foreach (DictionaryEntry item in source)
                yield return item;
        }
    }
}
