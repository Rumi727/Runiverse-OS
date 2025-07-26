#nullable enable
using RuniEngine.Collections;
using RuniEngine.Collections.Generic;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

using EditorGUI = UnityEditor.EditorGUI;
using EditorGUIUtility = UnityEditor.EditorGUIUtility;

namespace RuniEngine.Editor.Drawers.Collections.Generic
{
    [CustomPropertyDrawer(typeof(ISerializableDictionary<,,>), true)]
    public sealed class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        readonly Dictionary<string, SerializedProperty> cachedPairs = new();
        
        readonly Dictionary<string, AnimFloat> cachedAnimFloat = new();
        readonly Dictionary<string, ReorderableList> cachedReorderableList = new();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty? pairs = GetChildProperty(property);
            if (pairs == null)
            {
                GUI.Label(position, GetTextOrKey("serializable_dictionary_property_drawer.not_found.pairs"));
                return;
            }

            bool isInArray = property.IsInArray();

            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            ListHeader(position, pairs, label, index =>
            {
                pairs.InsertArrayElementAtIndex(index);

                //InsertArrayElementAtIndex 함수는 값을 복제하기 때문에 키를 기본값으로 정해줘야 제대로 생성할 수 있게 됨
                pairs.GetArrayElementAtIndex(index).SetDefaultValue();
            }, index => pairs.DeleteArrayElementAtIndex(index));

            position.y += headHeight + 2;

            if (!isInArray)
            {
                if (!cachedAnimFloat.TryGetValue(property.propertyPath, out AnimFloat animFloat))
                    return;

                if (pairs.isExpanded || animFloat.isAnimating)
                {
                    if (animFloat.isAnimating)
                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + animFloat.value));

                    ReorderableList reorderableList = GetReorderableList(property);
                    reorderableList.DoList(position);

                    if (animFloat.isAnimating)
                        GUI.EndClip();
                }

                if (animFloat.isAnimating)
                    RepaintCurrentWindow();
            }
            else if (pairs.isExpanded)
            {
                ReorderableList reorderableList = GetReorderableList(property);
                reorderableList.DoList(position);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            SerializedProperty? pairs = GetChildProperty(property);
            if (pairs == null)
                return base.GetPropertyHeight(property, label);

            float headerHeight = GetYSize(label, EditorStyles.foldoutHeader);
            float height;
            ReorderableList reorderableList = GetReorderableList(property);
            if (pairs.isExpanded)
                height = reorderableList.GetHeight() + 2;
            else
                height = 0;

            if (!property.IsInArray())
            {
                if (!cachedAnimFloat.TryGetValue(property.propertyPath, out AnimFloat animFloat))
                {
                    animFloat = new AnimFloat(height);
                    cachedAnimFloat[property.propertyPath] = animFloat;
                }
                
                animFloat.target = height;
                return animFloat.value + headerHeight;
            }
            else
                return height + headerHeight;
        }

        public SerializedProperty? GetChildProperty(SerializedProperty property)
        {
            if (!cachedPairs.TryGetValue(property.propertyPath, out SerializedProperty pairs))
            {
                pairs = property.FindPropertyRelative(nameof(ISerializableDictionary.pairs));
                cachedPairs[property.propertyPath] = pairs;
            }

            return pairs;
        }

        public ReorderableList GetReorderableList(SerializedProperty property)
        {
            if (!cachedReorderableList.TryGetValue(property.propertyPath, out ReorderableList reorderableList))
            {
                SerializedProperty? pairs = GetChildProperty(property);
                reorderableList = new ReorderableList(property.serializedObject, pairs)
                {
                    multiSelect = true,
                    headerHeight = 0,
                    drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        if (pairs == null)
                            return;
                        
                        SerializedProperty pairElement = pairs.GetArrayElementAtIndex(index);
                        SerializedProperty? pairKeyElement = GetKeyElement(pairElement);
                        object? lastValue = pairKeyElement?.boxedValue;
                        
                        rect.y += 1;

                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(rect, pairElement, GUIContent.none, pairs.IsGeneric());

                        //중복 감지
                        if (EditorGUI.EndChangeCheck() && pairKeyElement != null)
                        {
                            for (int i = 0; i < pairs.arraySize; i++)
                            {
                                if (index != i && Equals(GetKeyElement(pairs.GetArrayElementAtIndex(i))?.boxedValue, pairKeyElement.boxedValue))
                                {
                                    pairKeyElement.boxedValue = lastValue;
                                    break;
                                }
                            }
                        }
                    },
                    onAddCallback = x =>
                    {
                        if (pairs == null)
                            return;

                        int index = x.index + 1;

                        pairs.InsertArrayElementAtIndex(index);

                        //InsertArrayElementAtIndex 함수는 값을 복제하기 때문에 키를 기본값으로 정해줘야 제대로 생성할 수 있게 됨
                        pairs.GetArrayElementAtIndex(index).SetDefaultValue();

                        x.Select(index);
                        x.GrabKeyboardFocus();
                    },
                    onRemoveCallback = x =>
                    {
                        if (pairs == null)
                            return;

                        if (x.selectedIndices.Count > 0)
                        {
                            int removeCount = 0;
                            for (int i = 0; i < x.selectedIndices.Count; i++)
                            {
                                int index = x.selectedIndices[i] - removeCount;
                                if (index < 0 || index >= pairs.arraySize)
                                    continue;

                                pairs.DeleteArrayElementAtIndex(index);
                                removeCount++;
                            }
                        }
                        else
                            pairs.DeleteArrayElementAtIndex(pairs.arraySize - 1);

                        x.Select((x.index - 1).Clamp(0));
                        x.GrabKeyboardFocus();
                    },
                    onCanAddCallback = x =>
                    {
                        if (pairs == null)
                            return false;

                        for (int i = 0; i < pairs.arraySize; i++)
                        {
                            SerializedProperty? keyElement = GetKeyElement(pairs.GetArrayElementAtIndex(i));
                            if (keyElement == null)
                                continue;

                            if (keyElement.propertyType == SerializedPropertyType.String)
                            {
                                if (string.IsNullOrEmpty(keyElement.stringValue))
                                    return false;
                            }
                            else
                            {
                                object? boxedValue = keyElement.boxedValue;

                                if (boxedValue == null)
                                    return false;
                                if (boxedValue == boxedValue.GetType().GetDefaultValue())
                                    return false;
                            }
                        }

                        return true;
                    },
                    elementHeightCallback = i =>
                    {
                        if (pairs == null)
                            return EditorGUIUtility.singleLineHeight;

                        return EditorGUI.GetPropertyHeight(pairs.GetArrayElementAtIndex(i));
                    }
                };

                cachedReorderableList[property.propertyPath] = reorderableList;
            }
            
            return reorderableList;
        }
        
        static SerializedProperty? GetKeyElement(SerializedProperty pairProperty) => pairProperty.FindPropertyRelative(SerializableKeyValuePair.nameofKey);
    }
}
