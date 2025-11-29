#nullable enable
namespace RuniOS.Editor.IMGUI.Drawers
{
    [CustomPropertyDrawer(typeof(CornerRadius))]
    public class CornerRadiusPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) => Draw(position, property, label);
        
        public static void Draw(Rect position, SerializedProperty property, GUIContent label)
        {
            position.height = EditorGUIUtility.singleLineHeight;
            
            (SerializedProperty topLeftProperty, SerializedProperty topRightProperty, SerializedProperty bottomRightProperty, SerializedProperty bottomLeftProperty) = GetChildProperty(property);
            EditorGUI.BeginProperty(position, label, property.Copy());

            CornerRadius cornerRadius = new CornerRadius(topLeftProperty.floatValue, topRightProperty.floatValue, bottomRightProperty.floatValue, bottomLeftProperty.floatValue);
            
            {
                GetPrefixLabelRect(position, label, out _);
                property.isExpanded = EditorGUI.Foldout(new Rect(position.x, position.y, 0, position.height), property.isExpanded, GUIContent.none);

                EditorGUI.showMixedValue =
                    topLeftProperty.hasMultipleDifferentValues ||
                    topRightProperty.hasMultipleDifferentValues ||
                    bottomRightProperty.hasMultipleDifferentValues ||
                    bottomLeftProperty.hasMultipleDifferentValues ||
                    cornerRadius != cornerRadius.topLeft;
                
                EditorGUI.BeginChangeCheck();
                float radius = EditorGUI.FloatField(position, label, Min(cornerRadius.topLeft, cornerRadius.topRight, cornerRadius.bottomRight, cornerRadius.bottomLeft).Clamp(0));
                if (EditorGUI.EndChangeCheck())
                {
                    cornerRadius = radius;
                    WriteProperty();
                }

                EditorGUI.showMixedValue = false;
            }

            if (property.isExpanded)
            {
                position.y += EditorGUIUtility.singleLineHeight + 2;
                property.Next(true);
                
                BeginIndentLevel();
                EditorGUI.BeginChangeCheck();
                
                Field(GetTextOrKey("gui.top_left"), ref cornerRadius.topLeft);
                Field(GetTextOrKey("gui.top_right"), ref cornerRadius.topRight);
                Field(GetTextOrKey("gui.bottom_right"), ref cornerRadius.bottomRight);
                Field(GetTextOrKey("gui.bottom_left"), ref cornerRadius.bottomLeft);

                if (EditorGUI.EndChangeCheck())
                    WriteProperty();
                
                EndIndentLevel();

                void Field(string label, ref float value)
                {
                    EditorGUI.BeginProperty(position, new GUIContent(label), property.Copy());
                    value = EditorGUI.FloatField(position, label, value.Clamp(0)).Clamp(0);
                    EditorGUI.EndProperty();
                
                    position.y += EditorGUIUtility.singleLineHeight + 2;
                    property.Next(false);
                }
            }

            void WriteProperty()
            {
                topLeftProperty.floatValue = cornerRadius.topLeft;
                topRightProperty.floatValue = cornerRadius.topRight;
                bottomRightProperty.floatValue = cornerRadius.bottomRight;
                bottomLeftProperty.floatValue = cornerRadius.bottomLeft;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight + 2;

            if (property.isExpanded)
            {
                property.Next(true);

                for (int i = 0; i < 4; i++)
                {
                    height += EditorGUI.GetPropertyHeight(property, label) + 2;
                    property.Next(false);
                }
            }

            return height - 2;
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