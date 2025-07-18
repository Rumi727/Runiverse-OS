#nullable enable
using HarmonyLib;
using RuniEngine.Booting;
using System.Diagnostics;

namespace RuniEngine.Modding.UI
{
    public static partial class Patches
    {
        public static Harmony harmony = new Harmony("rumi.runios.ui");

        [Awaken]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken()
        {
            harmony.UnpatchSelf();
            HarmonyUtility.EditorPatchAll(harmony);
        }
    }
}
