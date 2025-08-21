#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using RuniOS.APIBridge.UnityEngine.UIElements;
using RuniOS.UIElements;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

using UniFoldout = UnityEngine.UIElements.Foldout;

namespace RuniOS.Patches
{
    public static partial class Patches
    {
        public static partial class UnityEngine
        {
            public static partial class UIElements
            {
                [HarmonyPatch(typeof(UniFoldout))]
                public static class Foldout
                {
                    static readonly ConditionalWeakTable<UniFoldout, AnimatedFadedGroup> fadedGroups = new();
                    
                    [HarmonyPatch]
                    public static class Constructor
                    {
                        public static MethodBase TargetMethod() => typeof(UniFoldout).Constructor();

                        public static void Postfix(UniFoldout __instance)
                        {
                            VisualElement content = __instance.contentContainer;
                            __instance.hierarchy.Remove(content);
                            
                            AnimatedFadedGroup animatedFadedGroup = new AnimatedFadedGroup(false, __instance, content);
                            animatedFadedGroup.SetValueWithoutNotify(__instance.value);

                            __instance.hierarchy.Add(animatedFadedGroup);
                            __instance.schedule.Execute(() =>
                            {
                                if (__instance.parent is BaseListView listView)
                                {
                                    float childHeight = 0;
                                    float maxHeight = listView.resolvedStyle.maxHeight.value;
                                    if (__instance.Q(classes: "unity-foldout__toggle") is { } toggle)
                                        maxHeight -= toggle.resolvedStyle.height;

                                    ScrollView listContent = BaseVerticalCollectionViewBridge.__GetInstanceFrom(listView).scrollView;
                                    if (listContent != null)
                                    {
                                        IResolvedStyle listContentStyle = listContent.resolvedStyle;
                                        
                                        childHeight += listContentStyle.marginBottom + listContentStyle.marginTop;
                                        childHeight += listContentStyle.paddingBottom + listContentStyle.paddingTop;
                                        childHeight += listContentStyle.borderBottomWidth + listContentStyle.borderTopWidth;
                                        
                                        if (listContent.Q(classes: "unity-scroll-view__content-container") is { } scrollViewContent)
                                            childHeight += scrollViewContent.resolvedStyle.height;

                                        if (listView.Q(classes: "unity-list-view__footer") is { } listViewFooter)
                                        {
                                            IResolvedStyle listViewFooterStyle = listViewFooter.resolvedStyle;
                                            childHeight += listViewFooterStyle.height;
                                            
                                            childHeight += listViewFooterStyle.marginBottom + listViewFooterStyle.marginTop;
                                            childHeight += listViewFooterStyle.paddingBottom + listViewFooterStyle.paddingTop;
                                            childHeight += listViewFooterStyle.borderBottomWidth + listViewFooterStyle.borderTopWidth;
                                        }
                                    }

                                    animatedFadedGroup.size = childHeight;
                                    animatedFadedGroup.maxSize = maxHeight;
                                    
                                    animatedFadedGroup.viewportSizeChange = true;
                                }
                                else
                                    animatedFadedGroup.viewportSizeChange = false;
                            }).Every(0);
                            
                            fadedGroups.Add(__instance, animatedFadedGroup);
                        }
                    }

                    [HarmonyTranspiler]
                    [HarmonyPatch("SetValueWithoutNotify")]
                    public static IEnumerable<CodeInstruction> SetValueWithoutNotifyTranspiler(IEnumerable<CodeInstruction> instructions)
                    {
                        CodeMatcher codeMatcher = new CodeMatcher(instructions);
                        codeMatcher.MatchStartForward(Code.Ldarg_0, Code.Callvirt[typeof(VisualElement).PropertyGetter(nameof(VisualElement.contentContainer))], Code.Callvirt[typeof(VisualElement).PropertyGetter(nameof(VisualElement.style))]);

                        int startIndex = codeMatcher.Pos;

                        if (!codeMatcher.IsValid)
                        {
                            Debug.LogWarning("Harmony Transpiler: Could not find IL in SetValueWithoutNotify.");
                            return instructions;
                        }

                        codeMatcher.MatchStartForward(Code.Callvirt[typeof(IStyle).PropertySetter("display")]);

                        if (!codeMatcher.IsValid)
                        {
                            Debug.LogWarning("Harmony Transpiler: Could not find IL in SetValueWithoutNotify.");
                            return instructions;
                        }

                        int endIndex = codeMatcher.Pos;

                        codeMatcher.RemoveInstructionsInRange(startIndex, endIndex);
                        return codeMatcher.InstructionEnumeration();
                    }

                    [HarmonyPostfix]
                    [HarmonyPatch("SetValueWithoutNotify")]
                    public static void SetValueWithoutNotifyPostfix(UniFoldout __instance)
                    {
                        if (fadedGroups.TryGetValue(__instance, out AnimatedFadedGroup? animatedFadedGroup))
                            animatedFadedGroup.SetValueWithoutNotify(__instance.value);
                    }
                }
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일