#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using System.Reflection;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using UnityEngine;
using RuniEngine.UI;

namespace RuniEngine.Modding.UI
{
    public static partial class Patches
    {
        public static class BaseSliderPatch
        {
            //슬라이더의 margin 값이 의도한대로 작동하게 수정
            [HarmonyPatch]
            public static class UpdateFillPatch
            {
                static IEnumerable<MethodBase> TargetMethods()
                {
                    // BaseSlider<> 제네릭 정의를 가져옵니다.
                    Type genericBaseSliderType = typeof(BaseSlider<>);

                    // Unity UI Toolkit이 사용하는 모든 기본 숫자 타입 (및 기타 관련 타입)을 여기에 추가합니다.
                    // 실제 사용되는 BaseSlider<T>의 T 타입들을 명시적으로 나열해야 합니다.
                    Type[] supportedTypes = new Type[]
                    {
                        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                        typeof(int), typeof(uint), typeof(long), typeof(ulong),
                        typeof(float), typeof(double), typeof(decimal)
                    };

                    foreach (Type t in supportedTypes)
                    {
                        // 닫힌 제네릭 타입을 생성합니다.
                        Type closedSliderType = genericBaseSliderType.MakeGenericType(t);

                        // 해당 닫힌 타입에서 "UpdateFill" 메서드를 찾습니다.
                        MethodBase method = AccessTools.DeclaredMethod(closedSliderType, "UpdateFill");
                        if (method != null)
                            yield return method;
                    }
                }

                static bool Prefix(object __instance, float normalizedValue)
                {
                    Type instanceType = __instance.GetType();

                    // 해당 닫힌 인스턴스 타입에 맞는 PropertyInfo를 가져옵니다.
                    PropertyInfo? fillProperty = AccessTools.Property(instanceType, "fill");
                    PropertyInfo? directionProperty = AccessTools.Property(instanceType, "direction");
                    PropertyInfo? invertedProperty = AccessTools.Property(instanceType, "inverted");
                    PropertyInfo? trackElementProperty = AccessTools.Property(instanceType, "trackElement");
                    FieldInfo? fillElementProperty = AccessTools.Field(instanceType, "<fillElement>k__BackingField");

                    if (fillProperty == null || directionProperty == null || invertedProperty == null || trackElementProperty == null || fillElementProperty == null)
                    {
                        Debug.LogWarning($"Warning: Required BaseSlider members not found for type {instanceType.Name}. Skipping patch.");
                        return true; // 원본 메서드 실행
                    }

                    bool fill = (bool)fillProperty.GetValue(__instance);
                    if (fill)
                    {
                        SliderDirection direction = (SliderDirection)directionProperty.GetValue(__instance);
                        VisualElement trackElement = (VisualElement)trackElementProperty.GetValue(__instance);
                        VisualElement fillElement = (VisualElement)fillElementProperty.GetValue(__instance);
                        bool inverted = (bool)invertedProperty.GetValue(__instance);

                        if (fillElement == null)
                        {
                            fillElement = new VisualElement
                            {
                                name = "unity-fill",
                                usageHints = UsageHints.DynamicColor
                            };

                            fillElement.AddToClassList(Slider.fillUssClassName);
                            trackElement.Add(fillElement);

                            fillElementProperty.SetValue(__instance, fillElement);
                        }

                        normalizedValue = 1 - normalizedValue;
                        if (direction == SliderDirection.Vertical)
                        {
                            Length length;
                            if (normalizedValue > 1)
                                length = Length.Percent(100);
                            else if (normalizedValue < 0)
                                length = Length.Percent(0);
                            else
                                length = Length.Pixels((trackElement.contentRect.height * normalizedValue) + (inverted ? trackElement.resolvedStyle.paddingBottom : trackElement.resolvedStyle.paddingTop));

                            fillElement.style.right = 0f;
                            fillElement.style.left = 0f;
                            fillElement.style.bottom = (inverted ? length : 0);
                            fillElement.style.top = (inverted ? 0 : length);
                        }
                        else
                        {
                            Length length;
                            if (normalizedValue > 1)
                                length = Length.Percent(100);
                            else if (normalizedValue < 0)
                                length = Length.Percent(0);
                            else
                                length = Length.Pixels(((trackElement.contentRect.width * normalizedValue) + (inverted ? trackElement.resolvedStyle.paddingLeft : trackElement.resolvedStyle.paddingRight)));

                            fillElement.style.top = 0f;
                            fillElement.style.bottom = 0f;
                            fillElement.style.left = (inverted ? length : 0);
                            fillElement.style.right = (inverted ? 0 : length);
                        }
                    }

                    return false; // 원본 UpdateFill 메서드 실행을 건너뜁니다.
                }
            }

            //슬라이더가 범위 밖으로 나가지 않게 수정
            [HarmonyPatch]
            public static class UpdateDragElementPositionPatch
            {
                static IEnumerable<MethodBase> TargetMethods()
                {
                    Type genericBaseSliderType = typeof(BaseSlider<>);

                    Type[] supportedTypes = new Type[]
                    {
                        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                        typeof(int), typeof(uint), typeof(long), typeof(ulong),
                        typeof(float), typeof(double), typeof(decimal)
                    };

                    foreach (Type t in supportedTypes)
                    {
                        Type closedSliderType = genericBaseSliderType.MakeGenericType(t);
                        MethodBase? method = AccessTools.DeclaredMethod(closedSliderType, "UpdateDragElementPosition", Array.Empty<Type>());

                        if (method != null)
                            yield return method;
                    }
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var codes = instructions.ToList();
                    int normalizeCallIndex = -1;

                    for (int i = 0; i < codes.Count; i++)
                    {
                        CodeInstruction code = codes[i];

                        if (code.opcode == OpCodes.Callvirt && code.operand is MethodInfo method && method.Name == "SliderNormalizeValue")
                        {
                            normalizeCallIndex = i;
                            break;
                        }
                    }

                    if (normalizeCallIndex >= 0)
                        codes.Insert(normalizeCallIndex + 1, CodeInstruction.Call(typeof(Mathf), "Clamp01"));
                    else
                        Debug.LogWarning("Harmony Transpiler: Could not find 'SliderNormalizeValue' call in UpdateDragElementPosition.");

                    return codes;
                }
            }

            //IUnclampedSlider 인터페이스를 상속하고있는 슬라이더는 Clamp 되지 않게 패치 및 outOfRangeUssClassName 같은 Uss 클래스 추가
            [HarmonyPatch]
            public static class ValueSetterChangePatch
            {
                static IEnumerable<MethodBase> TargetMethods()
                {
                    Type genericBaseSliderType = typeof(BaseSlider<>);

                    Type[] supportedTypes = new Type[]
                    {
                        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                        typeof(int), typeof(uint), typeof(long), typeof(ulong),
                        typeof(float), typeof(double), typeof(decimal)
                    };

                    foreach (Type t in supportedTypes)
                    {
                        Type closedSliderType = genericBaseSliderType.MakeGenericType(t);

                        MethodBase? method = AccessTools.DeclaredPropertySetter(closedSliderType, "value");
                        if (method != null)
                            yield return method;
                    }
                }

                static void Prefix(object __instance, ref object value)
                {
                    if (__instance is IUnclampedSlider unclampedSlider)
                    {
                        Type instanceType = __instance.GetType();
                        AccessTools.Property(instanceType, "clamped")?.SetValue(__instance, false);

                        value = unclampedSlider.GetClampedValue(value);
                    }
                }

                static void Postfix(object __instance)
                {
                    if (__instance is IUnclampedSlider unclampedSlider and VisualElement visualElement)
                    {
                        Type instanceType = __instance.GetType();
                        AccessTools.Property(instanceType, "clamped")?.SetValue(__instance, true);

                        visualElement.EnableInClassList(UnclampedSlider.outOfRangeUssClassName, unclampedSlider.isOutOfRange);
                        visualElement.EnableInClassList(UnclampedSlider.outOfLowUssClassName, unclampedSlider.isOutOfLow);
                        visualElement.EnableInClassList(UnclampedSlider.outOfHighUssClassName, unclampedSlider.isOutOfHigh);
                    }
                }
            }

            //텍스트 필드로 슬라이더를 수정할 때 Clamp 제거 (value 프로퍼티 자채의 Clamp는 건들지 않음)
            [HarmonyPatch]
            public static class OnTextFieldValueChangePatch
            {
                static IEnumerable<MethodBase> TargetMethods()
                {
                    Type genericBaseSliderType = typeof(BaseSlider<>);

                    Type[] supportedTypes = new Type[]
                    {
                        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                        typeof(int), typeof(uint), typeof(long), typeof(ulong),
                        typeof(float), typeof(double), typeof(decimal)
                    };

                    foreach (Type t in supportedTypes)
                    {
                        Type closedSliderType = genericBaseSliderType.MakeGenericType(t);

                        MethodBase? method = AccessTools.Method(closedSliderType, "OnTextFieldValueChange");
                        if (method != null)
                            yield return method;
                    }
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    var codes = instructions.ToList();

                    int ldargIndex = -1;
                    int callIndex = -1;

                    for (int i = 0; i < codes.Count; i++)
                    {
                        CodeInstruction code = codes[i];
                        if (code.opcode == OpCodes.Ldarg_0)
                        {
                            ldargIndex = i;
                            continue;
                        }
                        else if (code.opcode == OpCodes.Call && code.operand is MethodInfo method && method.Name == "GetClampedValue")
                        {
                            callIndex = i;
                            break;
                        }
                    }

                    if (ldargIndex >= 0 && callIndex >= 0)
                    {
                        codes.RemoveAt(ldargIndex);
                        codes.RemoveAt(callIndex - 1);
                    }
                    else
                        Debug.LogWarning("Harmony Transpiler: Could not find 'GetClampedValue' call in OnTextFieldValueChange.");

                    return codes;
                }
            }

            //스크롤바의 크기가 1이여도 숨겨지지 않게 패치
            [HarmonyPatch]
            public static class AdjustDragElementPatch
            {
                static IEnumerable<MethodBase> TargetMethods()
                {
                    Type genericBaseSliderType = typeof(BaseSlider<>);

                    Type[] supportedTypes = new Type[]
                    {
                        typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                        typeof(int), typeof(uint), typeof(long), typeof(ulong),
                        typeof(float), typeof(double), typeof(decimal)
                    };

                    foreach (Type t in supportedTypes)
                    {
                        Type closedSliderType = genericBaseSliderType.MakeGenericType(t);

                        MethodBase? method = AccessTools.Method(closedSliderType, "AdjustDragElement");
                        if (method != null)
                            yield return method;
                    }
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    CodeMatcher codeMatcher = new CodeMatcher(instructions);
                    codeMatcher.MatchStartForward(Code.Brfalse);

                    if (!codeMatcher.IsValid)
                    {
                        Debug.LogWarning("Harmony Transpiler: Could not find brfalse in AdjustDragElement.");
                        return instructions;
                    }

                    codeMatcher.RemoveInstructionsInRange(0, codeMatcher.Pos - 1);

                    codeMatcher.Start();
                    codeMatcher.MatchStartForward(Code.Brfalse);

                    if (!codeMatcher.IsValid)
                    {
                        Debug.LogWarning("Harmony Transpiler: Could not find brfalse in AdjustDragElement.");
                        return instructions;
                    }
                    
                    codeMatcher.Insert(Code.Ldc_I4_1);

                    return codeMatcher.Instructions();
                }

                static void Prefix(ref float factor) => factor = factor.Clamp01();

                static void Postfix(object __instance, float factor)
                {
                    if (__instance is VisualElement visualElement)
                        visualElement.EnableInClassList(RuniScrollView.scrollerOutOfRangeUssClassName, factor >= 1);
                }
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일