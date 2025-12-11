#nullable enable
using UnityEditor.Build;

namespace RuniOS.Editor
{
    [InitializeOnLoad]
    public static class ScriptingDefineSymbolSetter
    {
        static ScriptingDefineSymbolSetter()
        {
            const string symbolName = "RUNI_ENGINE";

            AddSymbol(NamedBuildTarget.Android, symbolName);
            AddSymbol(NamedBuildTarget.EmbeddedLinux, symbolName);
            AddSymbol(NamedBuildTarget.iOS, symbolName);
            AddSymbol(NamedBuildTarget.LinuxHeadlessSimulation, symbolName);
            AddSymbol(NamedBuildTarget.NintendoSwitch, symbolName);
            AddSymbol(NamedBuildTarget.PS4, symbolName);
            AddSymbol(NamedBuildTarget.PS5, symbolName);
            AddSymbol(NamedBuildTarget.Server, symbolName);
            AddSymbol(NamedBuildTarget.Standalone, symbolName);
            AddSymbol(NamedBuildTarget.tvOS, symbolName);
            AddSymbol(NamedBuildTarget.WebGL, symbolName);
            AddSymbol(NamedBuildTarget.WindowsStoreApps, symbolName);
            AddSymbol(NamedBuildTarget.XboxOne, symbolName);
            AddSymbol(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.GameCoreXboxSeries), symbolName); // Xbox Series X|S
            AddSymbol(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.GameCoreXboxOne), symbolName);  // Xbox One (GDK)
            AddSymbol(NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Kepler), symbolName);
#if UNITY_2022_1_OR_NEWER
            AddSymbol(NamedBuildTarget.QNX, symbolName);
#endif
#if !UNITY_2023_1_OR_NEWER
            AddSymbol(NamedBuildTarget.Stadia, symbolName);
#endif
#if UNITY_6000_1_OR_NEWER
            AddSymbol(NamedBuildTarget.VisionOS, symbolName);
            AddSymbol(NamedBuildTarget.NintendoSwitch2, symbolName);
#endif
        }

        static void AddSymbol(NamedBuildTarget target, string symbol)
        {
            string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(target);
            var defines = currentSymbols.Split(';', StringSplitOptions.RemoveEmptyEntries);
            if (!defines.Contains(symbol))
            {
                defines = defines.Add(symbol);
                PlayerSettings.SetScriptingDefineSymbols(target, string.Join(";", defines));
            }
        }
    }
}