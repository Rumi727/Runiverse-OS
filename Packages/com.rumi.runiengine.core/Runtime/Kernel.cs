#nullable enable
using RuniEngine.Booting;
using System;
using System.Diagnostics;
using UnityEngine;

namespace RuniEngine
{
    public static partial class Kernel
    {
        /*[GlobalData]
        public struct GlobalData
        {
            public static Version lastRuniEngineVersion { get; set; } = runiEngineVersion;
        }*/

        public static Version rosVersion { get; } = new Version(0, 0, 0);

        /// <summary>
        /// Application.version
        /// </summary>
        public static Version version
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                    return _version;
                else
                    return _version = Application.version;
            }
        }
        static Version _version = Version.zero;

        /// <summary>
        /// Application.unityVersion
        /// </summary>
        public static string unityVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_version))
                    return _unityVersion;
                else
                    return _unityVersion = Application.unityVersion;
            }
        }
        static string _unityVersion = "";



        /// <summary>
        /// Application.companyName
        /// </summary>
        public static string companyName
        {
            get
            {
                if (string.IsNullOrEmpty(_companyName))
                    return _companyName;
                else
                    return _companyName = Application.companyName;
            }
        }
        static string _companyName = "";

        /// <summary>
        /// Application.productName
        /// </summary>
        public static string productName
        {
            get
            {
                if (string.IsNullOrEmpty(_productName))
                    return _productName;
                else
                    return _productName = Application.productName;
            }
        }
        static string _productName = "";



        /// <summary>
        /// Application.platform
        /// </summary>
        public static RuntimePlatform platform { get; } = Application.platform;



        /// <summary>
        /// Application.internetReachability
        /// </summary>
        public static NetworkReachability internetReachability { get; private set; } = NetworkReachability.NotReachable;



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
        static void Awaken()
        {
            CustomPlayerLoopSetter.initEvent += Update;
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
        public static void Update() => TimeUpdate();

#pragma warning disable IDE0022 // 메서드에 식 본문 사용
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

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
            quitting?.Invoke();
            quitting = null;

            Application.quitting -= Quitting;

            /*AsyncTask.AllAsyncTaskCancel();

            if (UserAccountManager.currentAccount != null)
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
