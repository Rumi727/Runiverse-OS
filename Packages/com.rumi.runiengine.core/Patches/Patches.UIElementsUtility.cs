#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using System;
using System.Reflection;

namespace RuniEngine.Patches
{
    public static partial class Patches
    {
        public static class UIElementsUtilityPatch
        {
            public static Type TargetType() => AccessTools.TypeByName("UnityEngine.UIElements.UIElementsUtility");
            
            [HarmonyPatch]
            public static class BeginContainerGUI
            {
                public static MethodBase TargetMethod() => AccessTools.Method(TargetType(), "BeginContainerGUI");
                
                public static void Postfix()
                {
                    
                }
            }
            
            [HarmonyPatch]
            public static class EndContainerGUI
            {
                public static MethodBase TargetMethod() => AccessTools.Method(TargetType(), "EndContainerGUI");
                
                public static void Postfix()
                {
                    
                }
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일