#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using System;
using UnityEngine.UIElements;

namespace RuniEngine.Modding.UI
{
    public static partial class Patches
    {
        [HarmonyPatch(typeof(MinMaxSlider))]
        public static class MinMaxSliderPatch
        {
            //마진 값이 의도한대로 작동하게 수정합니다.
            [HarmonyPrefix]
            [HarmonyPatch("UpdateDragElementPosition", new Type[0])]
            static bool UpdateDragElementPosition(MinMaxSlider __instance)
            {
                if (__instance.panel != null)
                {
                    VisualElement dragElement = (VisualElement)AccessTools.DeclaredProperty(typeof(MinMaxSlider), "dragElement").GetValue(__instance);
                    VisualElement dragMinThumb = (VisualElement)AccessTools.DeclaredProperty(typeof(MinMaxSlider), "dragMinThumb").GetValue(__instance);
                    VisualElement dragMaxThumb = (VisualElement)AccessTools.DeclaredProperty(typeof(MinMaxSlider), "dragMaxThumb").GetValue(__instance);
                    VisualElement visualInput = (VisualElement)AccessTools.Property(typeof(MinMaxSlider), "visualInput").GetValue(__instance);

                    float minThumbOffset = dragMinThumb.resolvedStyle.marginLeft + dragMinThumb.resolvedStyle.marginRight;
                    float maxThumbOffset = dragMaxThumb.resolvedStyle.marginLeft + dragMaxThumb.resolvedStyle.marginRight;

                    float minThumbWidth = dragMinThumb.resolvedStyle.width + minThumbOffset;
                    float maxThumbWidth = dragMaxThumb.resolvedStyle.width;

                    float leftOffset = dragElement.resolvedStyle.borderLeftWidth + dragElement.resolvedStyle.marginLeft + minThumbOffset;
                    float rightOffset = dragElement.resolvedStyle.borderRightWidth + dragElement.resolvedStyle.marginRight + maxThumbOffset;
                    float offsetWidth = rightOffset + leftOffset;

                    float inputWidth = (visualInput.layout.width - offsetWidth - maxThumbWidth) + minThumbOffset;

                    float position = minThumbWidth.LerpUnclamped(inputWidth, SliderNormalizeValue(__instance.minValue));
                    float width = minThumbWidth.LerpUnclamped(inputWidth, SliderNormalizeValue(__instance.maxValue));

                    dragElement.style.width = width - position;
                    dragElement.style.left = position;
                    dragMinThumb.style.left = -dragMinThumb.resolvedStyle.width - leftOffset;
                    dragMaxThumb.style.right = -dragMaxThumb.resolvedStyle.width - rightOffset;

                    float SliderNormalizeValue(float currentValue) => (currentValue - __instance.lowLimit) / (__instance.highLimit - __instance.lowLimit);
                }

                return false;
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일