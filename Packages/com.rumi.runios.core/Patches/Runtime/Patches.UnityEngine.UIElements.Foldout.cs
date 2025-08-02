#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using RuniOS.UIElements;
using System.Collections.Generic;
using System.Linq;
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
                    static readonly ConditionalWeakTable<UniFoldout, VisualElement> viewportClippingElements = new();

                    [HarmonyPatch]
                    public static class Constructor
                    {
                        public static MethodBase TargetMethod() => typeof(UniFoldout).Constructor();

                        public static void Postfix(UniFoldout __instance)
                        {
                            __instance.styleSheets.Add(UIToolkitUtility.rosControlStyle);

                            VisualElement viewportClipping = new VisualElement
                            {
                                name = AnimFoldout.viewportClippingUssClassName
                            };
                            viewportClipping.AddToClassList(AnimFoldout.viewportClippingUssClassName);
                            __instance.hierarchy.Add(viewportClipping);

                            VisualElement viewport = new VisualElement
                            {
                                name = AnimFoldout.viewportUssClassName
                            };
                            viewport.AddToClassList(AnimFoldout.viewportUssClassName);
                            viewportClipping.hierarchy.Add(viewport);

                            VisualElement content = __instance.contentContainer;
                            __instance.hierarchy.Remove(content);
                            viewport.hierarchy.Add(content);

                            viewportClippingElements.Add(__instance, viewportClipping);
                            
                            content.RegisterCallback<GeometryChangedEvent>(x =>
                            {
                                if (__instance.value)
                                    viewportClipping.style.height = new Length(x.newRect.height.Max(1));
                                else
                                    viewportClipping.style.height = new Length(0);
                            });
                        }
                    }

                    [HarmonyTranspiler]
                    [HarmonyPatch("SetValueWithoutNotify")]
                    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
                    public static void SetValueWithoutNotify(UniFoldout __instance, bool newValue)
                    {
                        if (viewportClippingElements.TryGetValue(__instance, out VisualElement viewportClipping))
                        {
                            viewportClipping.UnregisterCallback<TransitionEndEvent, UniFoldout>(TransitionEndEvent);

                            if (viewportClipping.resolvedStyle.transitionDuration.Max().value <= 0)
                            {
                                __instance.contentContainer.style.display = newValue ? DisplayStyle.Flex : DisplayStyle.None;
                                viewportClipping.style.height = StyleKeyword.Null;
                            }
                            else
                            {
                                if (newValue)
                                {
                                    __instance.contentContainer.style.display = DisplayStyle.Flex;
                                    viewportClipping.style.height = new Length(__instance.contentContainer.resolvedStyle.height.Max(1));
                                }
                                else
                                {
                                    //__instance.contentContainer.style.display = DisplayStyle.None;
                                    viewportClipping.style.height = new Length(0);
                                    viewportClipping.RegisterCallbackOnce<TransitionEndEvent, UniFoldout>(TransitionEndEvent, __instance);
                                }
                            }
                        }

                        return;

                        static void TransitionEndEvent(TransitionEndEvent e, UniFoldout instance)
                        {
                            instance.contentContainer.style.display = DisplayStyle.None;
#if UNITY_EDITOR
                            // 레이아웃 갱신이 이미 애니메이션에 의해 이루어진 상태라 비활성화가 되어도 레이아웃이 갱신되지 않아 프리팹 바가 나타나는 버그를 수정합니다
                            UnityEditor.UIElements.InspectorElement inspector = instance.GetFirstAncestorOfType<UnityEditor.UIElements.InspectorElement>();
                            if (inspector != null)
                            {
                                instance.contentContainer.RegisterCallbackOnce<GeometryChangedEvent>(_ =>
                                {
                                    GeometryChangedEvent evt = GeometryChangedEvent.GetPooled(inspector.layout, inspector.layout);
                                    evt.target = inspector;
                                    
                                    inspector.SendEvent(evt);
                                });
                            }
#endif
                        }
                    }
                }
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일