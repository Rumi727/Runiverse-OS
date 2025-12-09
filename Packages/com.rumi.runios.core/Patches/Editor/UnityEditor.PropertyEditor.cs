#nullable enable
using HarmonyLib;
using RuniOS.Editor.APIBridge.UnityEditor;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Patches
{
    public static partial class Patches
    {
        public static partial class UnityEditorPatch
        {
            public static class PropertyEditorPatch
            {
                [HarmonyPatch]
                public static class RebuildContentsContainers
                {
                    public static MethodBase TargetMethod() => AccessTools.DeclaredMethod(PropertyEditorBridge.__targetType, nameof(PropertyEditorBridge.RebuildContentsContainers));

                    public static void Postfix(EditorWindow __instance)
                    {
                        if (!InspectorWindowBridge.__targetType.IsInstanceOfType(__instance))
                            return;
                        
                        PropertyEditorBridge propertyEditor = PropertyEditorBridge.__GetInstanceFrom(__instance);
                        InspectorWindowBridge inspectorWindow = InspectorWindowBridge.__GetInstanceFrom(__instance);
                        
                        IMGUIContainer imguiContainer = propertyEditor.CreateIMGUIContainer(() => InspectorWindowEvent._onGUI?.SafeInvoke(inspectorWindow), typeof(InspectorWindowEvent).FullName);
                        propertyEditor.editorsElement.Add(imguiContainer);
                    }
                }
            }
        }
    }
}