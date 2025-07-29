#nullable enable
using HarmonyLib;
using RuniEngine.Booting;

namespace RuniEngine.Patches
{
    public static partial class Patches
    {
        public static readonly Harmony harmony = new Harmony("Rumi.RuniOS");

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
