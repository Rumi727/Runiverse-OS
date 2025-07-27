#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniEngine.Patches
{
    public static partial class Patches
    {
        [HarmonyPatch(typeof(Foldout))]
        public static class FoldoutPatch
        {
            [HarmonyPatch]
            public static class Constructor
            {
                public static MethodBase TargetMethod() => typeof(Foldout).Constructor();
                
                public static void Postfix(Foldout __instance)
                {
                    __instance.styleSheets.Add(UIToolkitUtility.rosControlStyle);
                }
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일