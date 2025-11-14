#nullable enable
using RuniOS.Booting;
using RuniOS.LowLevel;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Scripting;

namespace RuniOS
{
    public static partial class Kernel
    {
        /*[GlobalData]
        public struct GlobalData
        {
            public static Version lastRuniEngineVersion { get; set; } = runiEngineVersion;
        }*/

        public static Version rosVersion { get; } = new Version(0, 0, 0);
        
#if UNITY_EDITOR
        /// <summary>
        /// Editor: Application.isEditor
        /// /
        /// Build: const false
        /// </summary>
        public static bool isEditor => Application.isEditor;

        /// <summary>
        /// Editor: Application.isPlaying
        /// /
        /// Build: const true
        /// </summary>
        public static bool isPlaying => Application.isPlaying;

        /// <summary>
        /// Editor: Application.isPlaying && !UnityEditor.EditorApplication.isPaused
        /// /
        /// Build: const true
        /// </summary>
        public static bool isPlayingAndNotPaused => Application.isPlaying && !UnityEditor.EditorApplication.isPaused;
#else
        public const bool isEditor = false;
        public const bool isPlaying = true;
        public const bool isPlayingAndNotPaused = true;
#endif



        /// <summary>
        /// Application.quitting 이벤트랑 동일하지만 커널보다 먼저 실행되는 것을 보장하며 플레이 모드 해제 시 이벤트가 자동으로 초기화됩니다
        /// </summary>
        public static event Action? quitting;



        [Awaken]
        [Preserve]
        static void Awaken()
        {
            RuniPlayerLoop.onInit += Update;
            Application.quitting += Quitting;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= Update;
            UnityEditor.EditorApplication.pauseStateChanged += PauseStateChanged;
#endif
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            if (!isPlaying)
                UnityEditor.EditorApplication.update += Update;
        }
#endif

#if UNITY_EDITOR
        static void PauseStateChanged(UnityEditor.PauseState pauseState) => deltaTimeStopwatch.Restart();
#endif

        static readonly Stopwatch deltaTimeStopwatch = Stopwatch.StartNew();
        static void Update() => TimeUpdate();

#pragma warning disable IDE0022 // 메서드에 식 본문 사용
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ReSharper disable once UnusedParameter.Global
        public static void Quit(int exitCode)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(exitCode);
#endif
        }
#pragma warning restore IDE0022 // 메서드에 식 본문 사용

        static void Quitting()
        {
            quitting.SafeInvoke();
            quitting = null;

            Application.quitting -= Quitting;
            
            /*if (UserAccountManager.currentAccount != null)
                UserAccountManager.LogoutWithoutUnload();

            if (BootLoader.isDataLoaded)
                BootLoader.globalData.SaveAll(globalDataPath);*/

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += Update;
            UnityEditor.EditorApplication.pauseStateChanged -= PauseStateChanged;
#endif
        }
    }
}
