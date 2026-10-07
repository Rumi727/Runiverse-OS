#nullable enable
using HarmonyLib;
using RuniOS.Modding;

namespace RuniOS.Editor.Patches
{
    public static partial class Patches
    {
        public static readonly Harmony harmony = new Harmony("Rumi.RuniOS.Editor");

        [InitializeOnLoadMethod]
        static void Awaken() => HarmonyUtility.PatchInEditor(harmony);
    }
}