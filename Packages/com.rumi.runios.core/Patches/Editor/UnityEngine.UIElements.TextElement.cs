#nullable enable
#pragma warning disable IDE1006 // 명명 스타일
using HarmonyLib;
using RuniOS.Editor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Patches;

public static partial class Patches
{
    public static partial class UnityEnginePatch
    {
        public static partial class UIElementsPatch
        {
            [HarmonyPatch(typeof(TextElement))]
            public static class TextElementPatch
            {
                [HarmonyPostfix]
                [HarmonyPatch(nameof(TextElement.text), MethodType.Setter)]
                public static void LabelSetter(TextElement __instance, string value)
                {
                    if (UIToolkitUtility.labelChangedCallbacks.TryGetValue(__instance, out Action<string> callback))
                        callback.Invoke(value);
                }
            }
        }
    }
}
#pragma warning restore IDE1006 // 명명 스타일