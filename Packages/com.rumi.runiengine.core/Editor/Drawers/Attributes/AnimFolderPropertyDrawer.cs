#nullable enable
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using static RuniEngine.Editor.EditorTool;
using EditorGUI = UnityEditor.EditorGUI;

namespace RuniEngine.Editor.Drawers.Attributes
{
    [CustomPropertyDrawer(typeof(AnimFolderAttribute))]
    public class AnimFolderPropertyDrawer : PropertyDrawer
    {
        AnimBool? animBool;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.IsChildrenIncluded() && !property.IsInArray() && (property.boxedValue == null || !property.boxedValue.GetType().IsAssignableToGenericDefinition(typeof(SerializableNullable<>))))
            {
                label = new GUIContent(label); //라벨 복제 안해주면 값 바뀜
                animBool ??= new AnimBool(property.isExpanded);
                
                {
                    if (animBool.isAnimating)
                    {
                        float headHeight = GetYSize(label, EditorStyles.foldout);
                        float childHeight = EditorGUI.GetPropertyHeight(property, label) - headHeight; //여기에서 값 바뀜

                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + headHeight + 3 + 0f.Lerp(childHeight, animBool.faded)));
                    }

                    EditorGUI.PropertyField(position, property, label, property.IsChildrenIncluded());

                    if (animBool.isAnimating)
                        GUI.EndClip();
                }

                if (animBool.isAnimating)
                    RepaintCurrentWindow();
            }
            else
                EditorGUI.PropertyField(position, property, label, property.IsChildrenIncluded());
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.IsChildrenIncluded() && !property.IsInArray())
            {
                animBool ??= new AnimBool(property.isExpanded);
                animBool.target = property.isExpanded;

                bool isExpanded = property.isExpanded;

                property.isExpanded = true;
                float childHeight = EditorGUI.GetPropertyHeight(property, label, true);
                property.isExpanded = isExpanded;

                float headHeight = GetYSize(label, EditorStyles.foldout) + 3;
                return headHeight.Lerp(childHeight, animBool.faded);
            }
            else
                return EditorGUI.GetPropertyHeight(property, label, property.IsChildrenIncluded());
        }
    }
}
