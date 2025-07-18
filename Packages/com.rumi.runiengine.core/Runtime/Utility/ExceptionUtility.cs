#nullable enable
using System;

namespace RuniEngine
{
    public static class ExceptionUtility
    {
        public static string ToSummaryString(this Exception e) => $"{e.GetType().Name}: {e.Message}\n\n{e.StackTrace.Substring(5)}";
    }
}
