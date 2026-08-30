#nullable enable
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniLayoutFields
    {
        public static CornerRadius CornerRadiusField(CornerRadius value, AnimFloat animFloat, bool isInArray = false) => RuniFields.CornerRadiusField(EditorGUILayout.GetControlRect(), value, animFloat, isInArray);
        public static CornerRadius CornerRadiusField(string label, CornerRadius value, AnimFloat animFloat, bool isInArray = false) => RuniFields.CornerRadiusField(EditorGUILayout.GetControlRect(), label, value, animFloat, isInArray);
        public static CornerRadius CornerRadiusField(GUIContent label, CornerRadius value, AnimFloat animFloat, bool isInArray = false) => RuniFields.CornerRadiusField(EditorGUILayout.GetControlRect(), label, value, animFloat, isInArray);
    }
}
