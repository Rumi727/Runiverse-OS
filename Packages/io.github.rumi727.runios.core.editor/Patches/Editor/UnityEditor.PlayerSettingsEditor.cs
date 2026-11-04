#nullable enable
using HarmonyLib;
using RuniOS.Editor.APIBridge.UnityEditor;
using System.Reflection;
using UnityEditor.Build;

namespace RuniOS.Editor.Patches
{
    public static partial class Patches
    {
        public static partial class UnityEditorPatch
        {
            /// <summary>
            /// CoreCLR 백엔드의 Api Compatibility Level 드롭다운에는 <c>NET_Standard</c>만 있어서,
            /// 숨겨진 <c>ApiCompatibilityLevel.NET</c>(7), <c>EditorAssembliesCompatibilityLevel.NET</c>(4)가 설정된 상태로 Player Settings가 그려지면 첫 항목으로 되돌아가고 재컴파일이 요청됩니다.
            /// 현재 값이 7이면 값 변경과 재컴파일 요청을 모두 막습니다.
            /// </summary>
            public static class PlayerSettingsEditorPatch
            {
                const ApiCompatibilityLevel net = (ApiCompatibilityLevel)7;
                const EditorAssembliesCompatibilityLevel editorNet = (EditorAssembliesCompatibilityLevel)4;

                [HarmonyPatch]
                public static class SetApiCompatibilityLevelForTarget
                {
                    public static MethodBase TargetMethod() => PlayerSettingsEditorBridge.__targetType.GetMethod(nameof(PlayerSettingsEditorBridge.SetApiCompatibilityLevelForTarget), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

                    public static bool Prefix(string targetGroup) => PlayerSettingsBridge.GetApiCompatibilityLevelInternal(targetGroup) != net;
                }

                [HarmonyPatch]
                public static class SetEditorAssembliesCompatibilityLevel
                {
                    public static MethodBase TargetMethod() => PlayerSettingsEditorBridge.__targetType.GetMethod(nameof(PlayerSettingsEditorBridge.SetEditorAssembliesCompatibilityLevel), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

                    public static bool Prefix() => PlayerSettings.GetEditorAssembliesCompatibilityLevel() != editorNet;
                }

                // GUI는 현재 플랫폼이 활성 빌드 플랫폼일 때만 이 이유를 추가합니다.
                [HarmonyPatch]
                public static class SetReason
                {
                    public static MethodBase TargetMethod() => PlayerSettingsEditorBridge.__targetType.GetMethod(nameof(PlayerSettingsEditorBridge.SetReason), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)!;

                    public static bool Prefix(string reason)
                    {
                        if (reason == PlayerSettingsEditorBridge.RecompileReasonBridge.apiCompatibilityLevelModified)
                        {
                            NamedBuildTarget activeBuildTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
                            return PlayerSettings.GetApiCompatibilityLevel(activeBuildTarget) != net;
                        }

                        if (reason == PlayerSettingsEditorBridge.RecompileReasonBridge.editorAssembliesCompatibilityLevelModified)
                            return PlayerSettings.GetEditorAssembliesCompatibilityLevel() != editorNet;

                        return true;
                    }
                }
            }
        }
    }
}
