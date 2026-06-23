#nullable enable
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RuniOS
{
    public static class Debug
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void Log(object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.Log(GetLogText(className, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void LogWarning(object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.LogWarning(GetLogText(className, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void LogError(object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.LogError(GetLogText(className, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void Assert(bool condition, object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.Assert(condition, GetLogText(className, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RuntimeLog(object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.Log(GetRuntimeLogText(className, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RuntimeLogWarning(object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.LogWarning(GetRuntimeLogText(className, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RuntimeLogError(object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.LogError(GetRuntimeLogText(className, message));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void RuntimeAssert(bool condition, object? message, string? className = null)
        {
            className ??= NameOfCallingClass();
            UnityEngine.Debug.Assert(condition, GetRuntimeLogText(className, message));
        }

        static string GetLogText(string className, object? message) => "[" + className + "] " + message;
        static string GetRuntimeLogText(string className, object? message) => "<b>[" + className + "]</b> " + message;


        // ReSharper disable Unity.PerformanceAnalysis
        public static void LogException(Exception exception) => UnityEngine.Debug.LogException(exception);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string NameOfCallingClass(int skipFrames = 0)
        {
            StackTrace stackTrace = new StackTrace(1, false);

            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                MethodBase? method = stackTrace.GetFrame(i)?.GetMethod();
                Type? type = GetLogicalDeclaringType(method);

                if (type == null)
                    continue;

                if (type == typeof(Debug))
                    continue;

                if (skipFrames > 0)
                {
                    skipFrames--;
                    continue;
                }

                return GetFullNameWithoutNamespace(type);
            }

            return nameof(Debug);
        }

        static Type? GetLogicalDeclaringType(MethodBase? method)
        {
            Type? type = method?.DeclaringType;

            while (type != null && IsCompilerGeneratedType(type) && type.DeclaringType != null)
            {
                type = type.DeclaringType;
            }

            return type;
        }

        static bool IsCompilerGeneratedType(Type type)
        {
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false))
                return true;

            if (typeof(IAsyncStateMachine).IsAssignableFrom(type))
                return true;

            return type.DeclaringType != null;
        }

        static string GetFullNameWithoutNamespace(Type type)
        {
            string fullName = type.FullName ?? type.Name;
            string? ns = type.Namespace;

            if (!string.IsNullOrEmpty(ns) && fullName.StartsWith(ns + ".", StringComparison.Ordinal))
                return fullName.Substring(ns.Length + 1);

            return fullName;
        }
    }
}