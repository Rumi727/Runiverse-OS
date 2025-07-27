#nullable enable
using System;
using System.Collections.Generic;
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
        readonly Dictionary<string, AnimFloat> cachedAnimFloat = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            try
            {
                if (property.IsGeneric() && !property.IsInArray() && !fieldInfo.FieldType.IsAssignableToGenericDefinition(typeof(ISerializableNullable<>))
                    && cachedAnimFloat.TryGetValue(property.propertyPath, out AnimFloat animFloat) && animFloat.isAnimating)
                {
                    label = new GUIContent(label); //라벨 복제 안해주면 값 바뀜

                    float orgHeight;
                    float headHeight = EditorGUIUtility.singleLineHeight;

                    //높이 계산
                    {
                        bool isExpanded = property.isExpanded;
                        property.isExpanded = true;

                        SerializedProperty childProperty = property.Copy();

                        orgHeight = EditorGUI.GetPropertyHeight(childProperty, label); //여기에서 값 바뀜
                        property.isExpanded = isExpanded;
                    }

                    //헤더
                    {
                        position.height = headHeight;

                        EditorGUI.BeginProperty(position, label, property);
                        property.isExpanded = EditorGUI.Foldout(position, property.isExpanded, label, true);
                        EditorGUI.EndProperty();
                    }

                    if (property.hasVisibleChildren)
                    {
                        float childHeight = orgHeight - headHeight;

                        position.y += headHeight + 2;

                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + (animFloat.value * childHeight)));

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

                        GUI.EndClip();
                    }

                    RepaintCurrentWindow();
                    return;
                }

                EditorGUI.PropertyField(position, property, label, property.IsGeneric());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.IsGeneric() && !property.IsInArray())
            {
                if (!cachedAnimFloat.TryGetValue(property.propertyPath, out AnimFloat animFloat))
                {
                    animFloat = new AnimFloat(property.isExpanded ? 1 : 0);
                    cachedAnimFloat[property.propertyPath] = animFloat;
                }
                animFloat.target = property.isExpanded ? 1 : 0;

                bool isExpanded = property.isExpanded;

                property.isExpanded = true;
                float childHeight = EditorGUI.GetPropertyHeight(property, label, true);
                property.isExpanded = isExpanded;

                float headHeight = GetYSize(label, EditorStyles.foldout) + 3;
                return headHeight.Lerp(childHeight, animFloat.value);
            }
            else
                return EditorGUI.GetPropertyHeight(property, label, property.IsGeneric());
        }
    }
}
