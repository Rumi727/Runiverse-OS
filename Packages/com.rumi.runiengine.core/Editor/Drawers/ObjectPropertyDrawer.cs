#nullable enable
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using static RuniEngine.Editor.EditorTool;
using EditorGUI = UnityEditor.EditorGUI;
using EditorGUIUtility = UnityEditor.EditorGUIUtility;

namespace RuniEngine.Editor.Drawers
{
    //[CustomPropertyDrawer(typeof(object), true)]
    public sealed class ObjectPropertyDrawer : PropertyDrawer
    {
        AnimBool? animBool;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.IsChildrenIncluded() && !property.IsInArray())
            {
                label = new GUIContent(label); //라벨 복제 안해주면 값 바뀜

                animBool ??= new AnimBool(property.isExpanded);
                
                float orgHeight;
                float headHeight = GetYSize(label, EditorStyles.foldout);
                
                //높이 계산
                {
                    bool isExpanded = property.isExpanded;
                    property.isExpanded = true;

                    SerializedProperty childProperty = property.Copy();
                    
                    orgHeight = EditorGUI.GetPropertyHeight(childProperty, label); //여기에서 값 바뀜
                    property.isExpanded = isExpanded;
                }

                position.y += 2;

                //헤더
                {
                    position.height = headHeight;

                    EditorGUI.BeginProperty(position, label, property);
                    property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
                    EditorGUI.EndProperty();
                }

                if (property.hasVisibleChildren && animBool.faded > 0)
                {
                    float childHeight = orgHeight - headHeight;

                    position.y += headHeight + 3;

                    if (animBool.isAnimating)
                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + 0f.Lerp(childHeight, animBool.faded)));

                    if (property.Next(true))
                    {
                        int depth = property.depth;
                        EditorGUI.indentLevel++;

                        do
                        {
                            position.height = EditorGUI.GetPropertyHeight(property);

                            BeginLabelWidth(EditorGUIUtility.labelWidth);
                            EditorGUI.PropertyField(position, property, false);
                            EndLabelWidth();

                            position.y += position.height + 2;
                        }
                        while (property.Next(false) && property.depth == depth);

                        EditorGUI.indentLevel--;
                    }

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
