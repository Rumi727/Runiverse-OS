#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniOS.Patches
{
    public static partial class Patches
    {
        public static partial class UnityEngine
        {
            public static partial class UIElements
            {
                public static class UIElementsUtility
                {
                    public static Type TargetType() => AccessTools.TypeByName("UnityEngine.UIElements.UIElementsUtility");
            
                    [HarmonyPatch]
                    public static class BeginContainerGUI
                    {
                        public static MethodBase TargetMethod() => AccessTools.Method(TargetType(), "BeginContainerGUI");
                
                        public static void Postfix(IMGUIContainer container) => IMGUIUtility.currentIMGUIContainer = container;
                    }
            
                    [HarmonyPatch]
                    public static class EndContainerGUI
                    {
                        public static MethodBase TargetMethod() => AccessTools.Method(TargetType(), "EndContainerGUI");
                
                        public static void Postfix() => IMGUIUtility.currentIMGUIContainer = null;
                    }
                }
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일