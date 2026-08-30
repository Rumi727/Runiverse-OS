#nullable enable
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.IMGUI
{
    public static partial class RuniFields
    {
        public static CornerRadius CornerRadiusField(Rect position, CornerRadius value, AnimFloat animFloat, bool isInArray = false) => DoCornerRadiusField(position, GUIContent.none, value, animFloat, isInArray);
        public static CornerRadius CornerRadiusField(Rect position, string label, CornerRadius value, AnimFloat animFloat, bool isInArray = false) => CornerRadiusField(position, new GUIContent(label), value, animFloat, isInArray);
        public static CornerRadius CornerRadiusField(Rect position, GUIContent label, CornerRadius value, AnimFloat animFloat, bool isInArray = false) => DoCornerRadiusField(position, label, value, animFloat, isInArray);

        static CornerRadius DoCornerRadiusField(Rect position, GUIContent label, CornerRadius value, AnimFloat animFloat, bool isInArray = false)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            bool isExpanded;
            {
                Rect foldoutRect;
                if (EditorGUIUtility.hierarchyMode)
                    foldoutRect = new Rect(position.x - 1, position.y, 0, position.height);
                else
                    foldoutRect = new Rect(position.x, position.y, 15, position.height);

                isExpanded = EditorGUI.Foldout(foldoutRect, animFloat.target > 0, GUIContent.none);
                EditorGUI.showMixedValue = value != value.topLeft;

                if (!EditorGUIUtility.hierarchyMode)
                    BeginIndentLevel();

                EditorGUI.BeginChangeCheck();
                float radius = EditorGUI.FloatField(position, label, Min(value.topLeft, value.topRight, value.bottomRight, value.bottomLeft).Clamp(0)).Clamp(0);
                if (EditorGUI.EndChangeCheck())
                    value = radius;

                EditorGUI.showMixedValue = false;
            }

            bool isAnimating = !isInArray && animFloat.isAnimating;
            float childHeight = (EditorGUIUtility.singleLineHeight + 2) * 4;

            animFloat.target = isExpanded ? childHeight : 0;

            if (isExpanded || isAnimating)
            {
                BeginIndentLevel();

                if (isAnimating)
                {
                    float fadedChildHeight = childHeight * animFloat.value;
                    GUI.BeginClip(new Rect(0, 0, position.x + position.width + (EditorGUI.indentLevel * 15) + 2, position.y + position.height + fadedChildHeight));
                }

                position.y += EditorGUIUtility.singleLineHeight + 2;

                Field(GetTextOrKey("gui.top_left"), ref value.topLeft);
                Field(GetTextOrKey("gui.top_right"), ref value.topRight);
                Field(GetTextOrKey("gui.bottom_right"), ref value.bottomRight);
                Field(GetTextOrKey("gui.bottom_left"), ref value.bottomLeft);

                void Field(string label, ref float value)
                {
                    value = EditorGUI.FloatField(position, label, value.Clamp(0)).Clamp(0);
                    position.y += EditorGUIUtility.singleLineHeight + 2;
                }

                if (isAnimating)
                    GUI.EndClip();

                EndIndentLevel();
            }

            if (!EditorGUIUtility.hierarchyMode)
                EndIndentLevel();

            if (isAnimating)
                RepaintCurrentWindow();

            return value;
        }
    }
}
