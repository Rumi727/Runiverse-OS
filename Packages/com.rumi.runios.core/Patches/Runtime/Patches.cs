#nullable enable
#if UNITY_EDITOR || !ENABLE_IL2CPP
using HarmonyLib;
using RuniOS.Booting;
using RuniOS.Modding;
using UnityEngine.Scripting;

namespace RuniOS.Patches
{
    public static partial class Patches
    {
        public static readonly Harmony harmony = new Harmony("Rumi.RuniOS");

        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => HarmonyUtility.PatchInEditor(harmony);
    }
}
#endif