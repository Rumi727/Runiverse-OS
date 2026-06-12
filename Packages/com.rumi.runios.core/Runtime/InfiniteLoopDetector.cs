#nullable enable
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace RuniOS
{
    public static class InfiniteLoopDetector
    {
        public const int detectionThreshold = 1000000;

        [Conditional("UNITY_EDITOR"), Conditional("UNITY_ENABLE_CHECKS")]
        public static void Run([CallerFilePath] string fp = "", [CallerLineNumber] int ln = 0, [CallerMemberName] string mn = "")
        {
            string currentPoint = $"{fp}:{ln}, {mn}()";
            if (isLoopDetected)
                throw new Exception($"Infinite Loop Detected: {currentPoint}");
        }

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
        public static bool isLoopDetected
        {
            get
            {
                if (++detectionCount > detectionThreshold)
                    return true;

                return false;
            }
        }
        static int detectionCount;
#else
        public const bool isLoopDetected = false;
#endif

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void Init() => UnityEditor.EditorApplication.update += Update;
#elif UNITY_ENABLE_CHECKS
        [Booting.Awaken]
        static void Awaken() => LowLevel.RuniPlayerLoop.onInit += Update;
#endif

#if UNITY_EDITOR || UNITY_ENABLE_CHECKS
        static void Update() => detectionCount = 0;
#endif
    }
}