#nullable enable
using HarmonyLib;
using RuniOS.Modding;
using Unity.Scripting.LifecycleManagement;

namespace RuniOS.Editor.Patches
{
    public static partial class Patches
    {
        public static readonly Harmony harmony = new Harmony("Rumi.RuniOS.Editor");

        [OnAssemblyLoaded]
        static void OnAssemblyLoaded() => HarmonyUtility.PatchInEditor(harmony, typeof(Patches).Assembly);

        // Patches keep references into this assembly, which would block it from unloading.
        [OnAssemblyUnloading]
        static void OnAssemblyUnloading() => HarmonyUtility.Unpatch(harmony);
    }
}
