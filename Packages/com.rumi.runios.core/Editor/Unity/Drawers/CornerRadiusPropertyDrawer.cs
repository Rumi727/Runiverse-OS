#nullable enable
using RuniOS.Editor.UIElements;
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.Unity.Drawers
{
    [CustomPropertyDrawer(typeof(CornerRadius))]
    public class CornerRadiusPropertyDrawer : PropertyDrawer
    {
        readonly Dictionary<string, AnimBool> cachedAnimBool = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;

            (SerializedProperty topLeftProperty, SerializedProperty topRightProperty, SerializedProperty bottomRightProperty, SerializedProperty bottomLeftProperty) = GetChildProperty(property);
            EditorGUI.BeginProperty(position, label, property.Copy());

            CornerRadius cornerRadius = new CornerRadius(topLeftProperty.floatValue, topRightProperty.floatValue, bottomRightProperty.floatValue, bottomLeftProperty.floatValue);

            {
                Rect foldoutRect;
                if (EditorGUIUtility.hierarchyMode)
                    foldoutRect = new Rect(position.x - 1, position.y, 0, position.height);
                else
                    foldoutRect = new Rect(position.x, position.y, 15, position.height);

                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, GUIContent.none);

                EditorGUI.showMixedValue =
                    topLeftProperty.hasMultipleDifferentValues ||
                    topRightProperty.hasMultipleDifferentValues ||
                    bottomRightProperty.hasMultipleDifferentValues ||
                    bottomLeftProperty.hasMultipleDifferentValues ||
                    cornerRadius != cornerRadius.topLeft;

                if (!EditorGUIUtility.hierarchyMode)
                    BeginIndentLevel();

                EditorGUI.BeginChangeCheck();
                float radius = EditorGUI.FloatField(position, label, Min(cornerRadius.topLeft, cornerRadius.topRight, cornerRadius.bottomRight, cornerRadius.bottomLeft).Clamp(0));
                if (EditorGUI.EndChangeCheck())
                {
                    cornerRadius = radius;
                    WriteProperty();
                }

                EditorGUI.showMixedValue = false;
            }

            AnimBool animBool = GetAnimBool(property);
            bool isAnimating = !property.IsInArray() && animBool.isAnimating;

            animBool.target = property.isExpanded;

            if (property.isExpanded || isAnimating)
            {
                BeginIndentLevel();

                if (isAnimating)
                {
                    float childHeight = GetChildHeight(property) * animBool.faded;
                    GUI.BeginClip(new Rect(0, 0, position.x + position.width + (EditorGUI.indentLevel * 15) + 2, position.y + position.height + childHeight));
                }

                position.y += EditorGUIUtility.singleLineHeight + 2;
                property.Next(true);

                EditorGUI.BeginChangeCheck();

                Field(GetTextOrKey("gui.top_left"), ref cornerRadius.topLeft);
                Field(GetTextOrKey("gui.top_right"), ref cornerRadius.topRight);
                Field(GetTextOrKey("gui.bottom_right"), ref cornerRadius.bottomRight);
                Field(GetTextOrKey("gui.bottom_left"), ref cornerRadius.bottomLeft);

                if (EditorGUI.EndChangeCheck())
                    WriteProperty();

                void Field(string label, ref float value)
                {
                    EditorGUI.BeginProperty(position, new GUIContent(label), property.Copy());
                    value = EditorGUI.FloatField(position, label, value.Clamp(0)).Clamp(0);
                    EditorGUI.EndProperty();

                    position.y += EditorGUIUtility.singleLineHeight + 2;
                    property.Next(false);
                }

                if (isAnimating)
                    GUI.EndClip();

                EndIndentLevel();
            }

            if (isAnimating)
                RepaintCurrentWindow();

            if (!EditorGUIUtility.hierarchyMode)
                EndIndentLevel();

            EditorGUI.EndProperty();

            void WriteProperty()
            {
                topLeftProperty.floatValue = cornerRadius.topLeft;
                topRightProperty.floatValue = cornerRadius.topRight;
                bottomRightProperty.floatValue = cornerRadius.bottomRight;
                bottomLeftProperty.floatValue = cornerRadius.bottomLeft;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            AnimBool animBool = GetAnimBool(property);
            bool isAnimating = !property.IsInArray() && animBool.isAnimating;

            float height = EditorGUIUtility.singleLineHeight;
            float childHeight = (property.isExpanded || isAnimating) ? GetChildHeight(property) : 0;

            height += (isAnimating ? childHeight * animBool.faded : childHeight);
            if (isAnimating)
                UIToolkitUtility.UpdateContainerHeight(height);

            return height;
        }

        AnimBool GetAnimBool(SerializedProperty property)
        {
            if (!cachedAnimBool.TryGetValue(property.propertyPath, out AnimBool animBool))
            {
                animBool = new AnimBool(property.isExpanded);
                cachedAnimBool[property.propertyPath] = animBool;
            }

            return animBool;
        }

        static float GetChildHeight(SerializedProperty property)
        {
            property = property.Copy();
            property.Next(true);

            float height = 0;
            for (int i = 0; i < 4; i++)
            {
                height += EditorGUI.GetPropertyHeight(property) + 2;
                property.Next(false);
            }

            return height;
        }

        public static (SerializedProperty topLeft, SerializedProperty topRight, SerializedProperty bottomRight, SerializedProperty bottomLeft) GetChildProperty(SerializedProperty property)
        {
            property = property.Copy();

            property.Next(true);
            SerializedProperty topLeft = property.Copy();

            property.Next(false);
            SerializedProperty topRight = property.Copy();

            property.Next(false);
            SerializedProperty bottomRight = property.Copy();

            property.Next(true);
            SerializedProperty bottomLeft = property.Copy();

            return (topLeft, topRight, bottomRight, bottomLeft);
        }
    }
}