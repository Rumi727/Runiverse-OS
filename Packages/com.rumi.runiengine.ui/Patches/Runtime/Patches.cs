#nullable enable
using HarmonyLib;
using RuniEngine.Booting;

namespace RuniEngine.Patches.UI
{
    public static partial class Patches
    {
        public static readonly Harmony harmony = new Harmony("Rumi.RuniOS.UI");

        [Awaken]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken()
        {
            harmony.UnpatchSelf();
            harmony.PatchAll();
        }
    }
}
