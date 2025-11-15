#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using RuniOS.Editor.UIElements;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Patches
{
    public static partial class Patches
    {
        public static partial class UnityEnginePatch
        {
            public static partial class UIElementsPatch
            {
                public static class UIElementsUtilityPatch
                {
                    public static Type targetType { get; } = AccessTools.TypeByName("UnityEngine.UIElements.UIElementsUtility");
            
                    [HarmonyPatch]
                    public static class BeginContainerGUI
                    {
                        public static MethodBase TargetMethod() => AccessTools.Method(targetType, nameof(BeginContainerGUI));
                
                        public static void Postfix(IMGUIContainer container) => UIToolkitUtility.currentIMGUIContainer = container;
                    }
            
                    [HarmonyPatch]
                    public static class EndContainerGUI
                    {
                        public static MethodBase TargetMethod() => AccessTools.Method(targetType, nameof(EndContainerGUI));
                
                        public static void Postfix() => UIToolkitUtility.currentIMGUIContainer = null;
                    }
                }
            }
        }
    }
#pragma warning restore IDE1006 // 명명 스타일
}