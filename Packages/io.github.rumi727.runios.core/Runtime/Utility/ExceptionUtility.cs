#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RuniOS.Utility
{
    public static class ExceptionUtility
    {
        public static string ToSummaryString(this Exception e) => $"{e.GetType().Name}: {e.Message}\n\n{e.StackTrace?.Substring(5)}";

        extension(ArgumentNullException)
        {
            /// <summary>
            /// Throws an <see cref="ArgumentNullException"/> if <paramref name="argument"/> is <see langword="null"/> or a destroyed Unity object.<br/>
            /// <see cref="ArgumentNullException.ThrowIfNull(object?, string?)"/>는 Unity 객체의 파괴 여부를 확인하지 않으므로, <paramref name="argument"/>가 <see langword="null"/>이거나 파괴된 Unity 객체이면 <see cref="ArgumentNullException"/>을 발생시킵니다.
            /// </summary>
            public static void ThrowIfUnityNull([NotNull] Object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
            {
                if (argument == null)
                    throw new ArgumentNullException(paramName);
            }
        }
    }
}