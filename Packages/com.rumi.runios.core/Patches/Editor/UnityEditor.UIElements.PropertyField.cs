#nullable enable
using HarmonyLib;
using RuniOS.Editor.UIElements;
using System.Reflection;
using System.Reflection.Emit;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Patches;

public static partial class Patches
{
    public static partial class UnityEditorPatch
    {
        public static partial class UIElementsPatch
        {
            [HarmonyPatch(typeof(PropertyField))]
            public static class PropertyFieldPatch
            {
                [HarmonyPatch]
                public static class Reset
                {
                    public static MethodBase TargetMethod() => AccessTools.DeclaredMethod(typeof(PropertyField), nameof(Reset), new Type[] { typeof(SerializedProperty) });

                    public static void Prefix(PropertyField __instance) => UIToolkitUtility.propertyExtensionDatas.Remove(__instance);

                    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
                    {
                        CodeMatcher matcher = new CodeMatcher(instructions, generator);
                        CodeMatch methodMatch = CodeMatch.Calls(typeof(PropertyDrawer).DeclaredMethod(nameof(PropertyDrawer.CreatePropertyGUI)));
                            
                        matcher.MatchStartForward(methodMatch);
                            
                        if (matcher.IsInvalid)
                        {
                            Debug.LogWarning($"Harmony Transpiler: Could not find insertion point for {nameof(PropertyDrawer.CreatePropertyGUI)} check in {nameof(Reset)}.");
                            return instructions;
                        }

                        matcher.Insert
                        (
                            Code.Ldarg_0, // this -> propertyField
                            Code.Call[ReflectionUtility.GetMethodInfo((Action<PropertyField>)PreCreatePropertyGUI)]
                        );
                            
                        matcher.MatchStartForward(methodMatch);
                        matcher.Advance(1);
                            
                        matcher.Insert
                        (
                            Code.Ldarg_0, // this -> propertyField
                            Code.Call[ReflectionUtility.GetMethodInfo((Action<PropertyField>)PostCreatePropertyGUI)]
                        );
                            
                        return matcher.InstructionEnumeration();
                    }
                }
                    
                /*[HarmonyPatch]
                public static class ResetDecoratorDrawers
                {
                    public static MethodBase TargetMethod() => AccessTools.DeclaredMethod(typeof(global::UnityEditor.UIElements.PropertyField), nameof(ResetDecoratorDrawers));

                    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
                    {
                        CodeMatcher matcher = new CodeMatcher(instructions, generator);
                        CodeMatch methodMatch = CodeMatch.Calls(typeof(global::UnityEditor.DecoratorDrawer).DeclaredMethod(nameof(global::UnityEditor.DecoratorDrawer.CreatePropertyGUI)));

                        matcher.MatchStartForward(methodMatch);

                        if (matcher.IsInvalid)
                        {
                            Debug.LogWarning($"Harmony Transpiler: Could not find insertion point for {nameof(global::UnityEditor.DecoratorDrawer.CreatePropertyGUI)} check in {nameof(ResetDecoratorDrawers)}.");
                            return instructions;
                        }

                        matcher.Insert
                        (
                            Code.Ldarg_0, // this -> propertyField
                            Code.Call[ReflectionUtility.GetMethodInfo((Action<global::UnityEditor.UIElements.PropertyField>)PreCreatePropertyGUI)]
                        );

                        matcher.MatchEndForward(methodMatch);

                        matcher.Insert
                        (
                            Code.Ldarg_0, // this -> propertyField
                            Code.Call[ReflectionUtility.GetMethodInfo((Action<global::UnityEditor.UIElements.PropertyField>)PostCreatePropertyGUI)]
                        );

                        return matcher.InstructionEnumeration();
                    }
                }*/

                [HarmonyPostfix]
                [HarmonyPatch("CreateFoldout")]
                public static void PostfixCreateFoldout(PropertyField __instance, VisualElement __result) => UIToolkitUtility.propertyExtensionDatas.GetOrCreateValue(__instance).foldout = (Foldout)__result;

                public static void PreCreatePropertyGUI(PropertyField propertyField) => UIToolkitUtility._currentPropertyField.Push(propertyField);

                public static void PostCreatePropertyGUI(PropertyField propertyField) => UIToolkitUtility._currentPropertyField.Pop();
            }
        }
    }
}