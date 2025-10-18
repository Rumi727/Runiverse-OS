#nullable enable
using RuniOS;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
public static class Debug
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Log(GetLogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogWarning(GetLogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogError(GetLogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Assert(bool condition, object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Assert(condition, GetLogText(className, message));
    }

    public static void RuntimeLog(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Log(GetRuntimeLogText(className, message));
    }

    public static void RuntimeLogWarning(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogWarning(GetRuntimeLogText(className, message));
    }

    public static void RuntimeLogError(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogError(GetRuntimeLogText(className, message));
    }

    public static void RuntimeAssert(bool condition, object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Assert(condition, GetRuntimeLogText(className, message));
    }

    static string GetLogText(string className, object? message) => "[" + className + "] " + message;
    static string GetRuntimeLogText(string className, object? message) => "<b>[" + className + "]</b> " + message;


    // ReSharper disable Unity.PerformanceAnalysis
    public static void LogException(Exception exception) => UnityEngine.Debug.LogException(exception);

    public static string NameOfCallingClass(int skipFrames = 0)
    {
        skipFrames += 2;

        StackTrace stackTrace = new StackTrace();
        if (stackTrace.FrameCount > skipFrames)
        {
            StackFrame stackFrame = stackTrace.GetFrame(skipFrames);
            MethodBase methodBase = stackFrame.GetMethod();
            Type type = methodBase.DeclaringType ?? typeof(Debug);

            if (type.IsCompilerGenerated())
            {
                string name = type.FullName ?? string.Empty;

                const string pattern = @"\.(.*?)\+"; 
                Match matches = Regex.Match(name, pattern);
                
                if (matches.Groups.Count > 1)
                    return matches.Groups[1].Value;
                else
                    return name;
            }

            return type.Name;
        }
        else
            return nameof(Debug);
    }

    public static StackFrame GetMethodCallerStackFrame()
    {
        StackFrame stackFrame;
        Type? declaringType;
        int skipFrames = 2;
        do
        {
            stackFrame = new StackFrame(skipFrames, true);
            declaringType = stackFrame.GetMethod().DeclaringType;

            if (declaringType == null)
                return stackFrame;

            skipFrames++;
        }
        while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

        return stackFrame;
    }
}