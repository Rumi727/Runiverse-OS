#nullable enable
using UnityEditor;
using UnityEditor.Build;

namespace RuniEngine.Editor
{
    [InitializeOnLoad]
    public static class ScriptingDefineSymbolSetter
    {
        static ScriptingDefineSymbolSetter()
        {
            const string symbolName = "RUNI_ENGINE";

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.EmbeddedLinux, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.iOS, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.LinuxHeadlessSimulation, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.NintendoSwitch, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.PS4, symbolName);
#if UNITY_2023_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.PS5, symbolName);
#endif
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.QNX, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Server, symbolName);
#if !UNITY_2023_1_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Stadia, symbolName);
#endif
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.tvOS, symbolName);
#if UNITY_6000_1_ORNEWER
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.VisionOS, symbolName);
#endif
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WindowsStoreApps, symbolName);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.XboxOne, symbolName);
        }
    }
}