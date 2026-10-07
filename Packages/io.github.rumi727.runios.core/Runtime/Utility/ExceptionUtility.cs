#nullable enable
using System.Diagnostics.CodeAnalysis;

namespace RuniOS.Utility
{
    public static class ExceptionUtility
    {
        public static string ToSummaryString(this Exception e) => $"{e.GetType().Name}: {e.Message}\n\n{e.StackTrace.Substring(5)}";
        
        public static void ThrowIfArgumentNull([NotNull] object? argument, string? paramName = null)
        {
            if (argument is null)
                throw new ArgumentNullException(paramName ?? nameof(argument));
        }
        
        public static void ThrowIfArgumentNull([NotNull] Object? argument, string? paramName = null)
        {
            if (argument == null)
                throw new ArgumentNullException(paramName ?? nameof(argument));
        }
    }
}