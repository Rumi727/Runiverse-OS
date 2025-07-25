#nullable enable
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RuniEngine
{
    public static class InfiniteLoopDetector
    {
        [ThreadStatic] static int detectionCount;
        public const int detectionThreshold = 1000000;

        [Conditional("UNITY_EDITOR")]
        public static void Run([CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0, [CallerMemberName] string mn = "")
        {
            string currentPoint = $"{fp}:{ln}, {mn}()";
            if (isLoopDetected)
                throw new Exception($"Infinite Loop Detected: {currentPoint}");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public static bool isLoopDetected
        {
            get
            {
                if (++detectionCount > detectionThreshold)
                    return true;

                return false;
            }
        }
#else
        public const bool isLoopDetected = false;
#endif

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void Init() => UnityEditor.EditorApplication.update += static () => detectionCount = 0;
#elif DEVELOPMENT_BUILD
        static void Awaken() => Booting.CustomPlayerLoopSetter.initEvent += static () => detectionCount = 0;
#endif
    }
}