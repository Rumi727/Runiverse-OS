#nullable enable
using HarmonyLib;
using RuniEngine.Booting;
using System.Diagnostics;
using UnityEngine.Scripting;

namespace RuniEngine.Patches.UI
{
    public static partial class Patches
    {
        public static readonly Harmony harmony = new Harmony("Rumi.RuniOS.UI");

        [Awaken]
        [Preserve]
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#endif
        static void Awaken()
        {
            Debug.Log("Patch");
            Stopwatch stopwatch = Stopwatch.StartNew();
            harmony.UnpatchSelf();
            Debug.Log(stopwatch.Elapsed.TotalSeconds);
            stopwatch = Stopwatch.StartNew();
            harmony.PatchAll();
            Debug.Log(stopwatch.Elapsed.TotalSeconds);
        }
    }
}
