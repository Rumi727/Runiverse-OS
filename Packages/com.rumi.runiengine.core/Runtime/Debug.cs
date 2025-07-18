#nullable enable
using RuniEngine;
using System;
using System.Diagnostics;
using System.Reflection;

public static class Debug
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Log(LogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogWarning(LogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogError(LogText(className, message));
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Assert(bool condition, object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Assert(condition, LogText(className, message));
    }

    public static void ForceLog(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Log(ForceLogText(className, message));
    }

    public static void ForceLogWarning(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogWarning(ForceLogText(className, message));
    }

    public static void ForceLogError(object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.LogError(ForceLogText(className, message));
    }

    public static void ForceAssert(bool condition, object? message, string? className = null)
    {
        className ??= NameOfCallingClass();
        UnityEngine.Debug.Assert(condition, ForceLogText(className, message));
    }

    static string LogText(string className, object? message) => "[" + className + "] " + message;
    static string ForceLogText(string className, object? message) => "<b>[" + className + "]</b> " + message;


    public static void LogException(Exception exception) => UnityEngine.Debug.LogException(exception);

    public static string NameOfCallingClass(int skipFrames = 0)
    {
        skipFrames += 2;

        StackTrace stackTrace = new StackTrace();
        if (stackTrace.FrameCount > skipFrames)
        {
            StackFrame stackFrame = stackTrace.GetFrame(skipFrames);
            MethodBase methodBase = stackFrame.GetMethod();
            Type type = methodBase.DeclaringType;

            if (type.IsCompilerGenerated())
            {
                string name = type.FullName;
                int startIndex = name.LastIndexOf('.') + 1;

                return type.FullName.Substring(startIndex, name.LastIndexOf('+') - startIndex);
            }

            return type.Name;
        }
        else
            return nameof(Debug);
    }

    public static StackFrame GetMethodCallerStackFrame()
    {
        StackFrame stackFrame;
        Type declaringType;
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