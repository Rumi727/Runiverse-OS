#nullable enable
using HarmonyLib;
using System.Diagnostics;
using System.Reflection;

namespace RuniOS.Modding
{
    /// <summary>
    /// Harmony 패치 관련 유틸리티 메서드를 제공합니다.<br/>
    /// 이 클래스는 Harmony 패치를 적용하거나 제거하는 데 사용됩니다.
    /// </summary>
    public static class HarmonyUtility
    {
#if UNITY_EDITOR
        public static bool logEnable => Editor.Modding.ModdingConfigAsset.instance.logInEditor;
#else
    public static bool logEnable => true;
#endif
        
        /// <summary>
        /// 에디터 환경에서는 패치를 다시 적용하지만, 빌드된 환경에서는 최적화를 위해 기존 패치를 제거하지 않습니다.<br/>
        /// </summary>
        /// <exception cref="System.ArgumentNullException"> <paramref name="harmony"/>가 <see langword="null"/>인 경우 발생합니다.</exception>
        public static void PatchInEditor(Harmony harmony, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
#if UNITY_EDITOR
            Repatch(harmony, assembly);
#else
        Patch(harmony, assembly);
#endif
        }

        /// <summary>
        /// 지정된 <see cref="Harmony"/> 인스턴스의 모든 패치를 적용합니다.<br/>
        /// 이 메서드는 기존 패치를 제거하지 않고 새로운 패치만 적용합니다.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"> <paramref name="harmony"/>가 <see langword="null"/>인 경우 발생합니다.</exception>
        public static void Patch(Harmony harmony, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
            if (logEnable)
                Debug.Log($"[{harmony.Id}] Patching operations started.");

            Stopwatch stopwatch = Stopwatch.StartNew();
            harmony.PatchAll(assembly);
            stopwatch.Stop();
            
            if (logEnable)
                Debug.Log($"[{harmony.Id}] All patches applied in {stopwatch.Elapsed.TotalSeconds:F4} seconds.");
        }

        /// <summary>
        /// 지정된 <see cref="Harmony"/> 인스턴스의 모든 패치를 제거합니다.<br/>
        /// 이 메서드는 현재 적용된 모든 패치를 원상복구합니다.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"> <paramref name="harmony"/>가 <see langword="null"/>인 경우 발생합니다.</exception>
        public static void Unpatch(Harmony harmony)
        {
            if (logEnable)
                Debug.Log($"[{harmony.Id}] Unpatching operations started.");

            Stopwatch stopwatch = Stopwatch.StartNew();
            harmony.UnpatchSelf();
            stopwatch.Stop();
            
            if (logEnable)
                Debug.Log($"[{harmony.Id}] All patches removed in {stopwatch.Elapsed.TotalSeconds:F4} seconds.");
        }

        /// <summary>
        /// 지정된 <see cref="Harmony"/> 인스턴스의 기존 패치를 제거한 후 모든 패치를 다시 적용합니다.<br/>
        /// 주로 개발 환경에서 패치 변경 사항을 빠르게 반영할 때 유용합니다.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"> <paramref name="harmony"/>가 <see langword="null"/>인 경우 발생합니다.</exception>
        public static void Repatch(Harmony harmony, Assembly? assembly = null)
        {
            assembly ??= Assembly.GetCallingAssembly();
            
            if (logEnable)
                Debug.Log($"[{harmony.Id}] Patching operations started.");

            Stopwatch stopwatch = Stopwatch.StartNew();
            harmony.UnpatchSelf();
            stopwatch.Stop();
            
            if (logEnable)
                Debug.Log($"[{harmony.Id}] Existing patches removed in {stopwatch.Elapsed.TotalSeconds:F4} seconds.");

            stopwatch.Restart();
            harmony.PatchAll(assembly);
            stopwatch.Stop();
            
            if (logEnable)
                Debug.Log($"[{harmony.Id}] All patches applied in {stopwatch.Elapsed.TotalSeconds:F4} seconds.");
        }
    }
}