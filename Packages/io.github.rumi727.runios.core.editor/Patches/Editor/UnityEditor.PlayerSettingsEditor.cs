#nullable enable
using HarmonyLib;
using System.Reflection;

namespace RuniOS.Editor.Patches
{
    public static partial class Patches
    {
        public static partial class UnityEditorPatch
        {
            public static class PlayerSettingsEditorPatch
            {
                /// <summary>
                /// CoreCLR 백엔드의 Api Compatibility Level 드롭다운에는 <c>NET_Standard</c>만 있어서,
                /// 숨겨진 <c>ApiCompatibilityLevel.NET</c>(7)이 설정된 상태로 Player Settings가 그려지면 첫 항목으로 되돌아갑니다.
                /// 현재 값이 7이면 이 되돌림을 막습니다.
                /// </summary>
                [HarmonyPatch]
                public static class SetApiCompatibilityLevelForTarget
                {
                    const ApiCompatibilityLevel net = (ApiCompatibilityLevel)7;

                    static readonly Func<string, ApiCompatibilityLevel> getApiCompatibilityLevel = AccessTools.MethodDelegate<Func<string, ApiCompatibilityLevel>>(AccessTools.DeclaredMethod(typeof(PlayerSettings), "GetApiCompatibilityLevelInternal"));

                    public static MethodBase TargetMethod() => AccessTools.DeclaredMethod(typeof(PlayerSettings).Assembly.GetType("UnityEditor.PlayerSettingsEditor", true), nameof(SetApiCompatibilityLevelForTarget));

                    public static bool Prefix(string targetGroup) => getApiCompatibilityLevel(targetGroup) != net;
                }
            }
        }
    }
}
