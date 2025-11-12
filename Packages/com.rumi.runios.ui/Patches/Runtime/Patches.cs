#nullable enable
using HarmonyLib;
using RuniOS.Booting;
using RuniOS.Modding;
using UnityEngine.Scripting;

namespace RuniOS.Patches.UI
{
    public static partial class Patches
    {
        public static readonly Harmony harmony = new Harmony("Rumi.RuniOS.UI");

        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken() => HarmonyUtility.PatchInEditor(harmony);
    }
}
