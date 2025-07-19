#nullable enable
using RuniEngine.Collections;
using RuniEngine.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;

using EditorGUI = UnityEditor.EditorGUI;
using EditorGUIUtility = UnityEditor.EditorGUIUtility;

namespace RuniEngine.Editor.Drawers.Collections
{
    [CustomPropertyDrawer(typeof(ISerializableDictionary<,>), true)]
    public sealed class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        AnimFloat? animFloat;
        ReorderableList? reorderableList;

        SerializedProperty? key;
        SerializedProperty? value;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InitListProperty(property);
            if (key == null)
            {
                GUI.Label(position, TryGetText("serializable_dictionary_property_drawer.not_found.key"));
                return;
            }
            else if (value == null)
            {
                GUI.Label(position, TryGetText("serializable_dictionary_property_drawer.not_found.value"));
                return;
            }

            bool isInArray = property.IsInArray();

            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            ListHeader(position, key, label, index =>
            {
                key.InsertArrayElementAtIndex(index);
                value.InsertArrayElementAtIndex(index);

                //InsertArrayElementAtIndex 함수는 값을 복제하기 때문에 키를 기본값으로 정해줘야 제대로 생성할 수 있게 됨
                key.GetArrayElementAtIndex(index).SetDefaultValue();
            }, index =>
            {
                key.DeleteArrayElementAtIndex(index);
                value.DeleteArrayElementAtIndex(index);
            });

            position.y += headHeight + 2;

            if (!isInArray)
            {
                if (animFloat == null)
                    return;

                if (key.isExpanded || animFloat.isAnimating)
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
            else if (key.isExpanded)
            {
                ReorderableList reorderableList = GetReorderableList(property);
                reorderableList.DoList(position);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitListProperty(property);
            if (key == null || value == null)
                return base.GetPropertyHeight(property, label);

            float headerHeight = GetYSize(label, EditorStyles.foldoutHeader);
            float height;
            ReorderableList reorderableList = GetReorderableList(property);
            if (key.isExpanded)
                height = reorderableList.GetHeight() + 2;
            else
                height = 0;

            if (!property.IsInArray())
            {
                animFloat ??= new AnimFloat(height);
                animFloat.target = height;

                return animFloat.value + headerHeight;
            }
            else
                return height + headerHeight;
        }

        public void InitListProperty(SerializedProperty property)
        {
            key ??= property.FindPropertyRelative(nameof(ISerializableDictionary.serializableKeys));
            value ??= property.FindPropertyRelative(nameof(ISerializableDictionary.serializableValues));
        }

        public ReorderableList GetReorderableList(SerializedProperty property)
        {
            return reorderableList ??= new ReorderableList(property.serializedObject, key)
            {
                multiSelect = true,
                headerHeight = 0,
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    if (key == null || value == null)
                        return;

                    string keyLabel = TryGetText("gui.key");
                    string valueLabel = TryGetText("gui.value");

                    rect.width /= 2;
                    rect.width -= 10;

                    BeginLabelWidth(keyLabel);

                    SerializedProperty keyElement = key.GetArrayElementAtIndex(index);
                    object? lastValue = keyElement.boxedValue;

                    rect.height = EditorGUI.GetPropertyHeight(keyElement);

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(rect, keyElement, new GUIContent(keyLabel), keyElement.IsChildrenIncluded());

                    //중복 감지
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 0; i < key.arraySize; i++)
                        {
                            if (index != i && Equals(key.GetArrayElementAtIndex(i).boxedValue, keyElement.boxedValue))
                            {
                                keyElement.boxedValue = lastValue;
                                break;
                            }
                        }
                    }

                    EndLabelWidth();

                    rect.x += rect.width + 20;

                    BeginLabelWidth(valueLabel);

                    SerializedProperty valueElement = value.GetArrayElementAtIndex(index);
                    rect.height = EditorGUI.GetPropertyHeight(valueElement);

                    EditorGUI.PropertyField(rect, valueElement, new GUIContent(valueLabel), valueElement.IsChildrenIncluded());

                    EndLabelWidth();
                },
                onAddCallback = x =>
                {
                    if (key == null || value == null)
                        return;

                    int index = x.index + 1;

                    key.InsertArrayElementAtIndex(index);
                    value.InsertArrayElementAtIndex(index);

                    //InsertArrayElementAtIndex 함수는 값을 복제하기 때문에 키를 기본값으로 정해줘야 제대로 생성할 수 있게 됨
                    key.GetArrayElementAtIndex(index).SetDefaultValue();
                    value.GetArrayElementAtIndex(index).SetDefaultValue();

                    x.Select(index);
                    x.GrabKeyboardFocus();
                },
                onRemoveCallback = x =>
                {
                    if (key == null || value == null)
                        return;

                    if (x.selectedIndices.Count > 0)
                    {
                        int removeCount = 0;
                        for (int i = 0; i < x.selectedIndices.Count; i++)
                        {
                            int index = x.selectedIndices[i] - removeCount;
                            if (index < 0 || index >= key.arraySize)
                                continue;

                            key.DeleteArrayElementAtIndex(index);
                            value.DeleteArrayElementAtIndex(index);

                            removeCount++;
                        }
                    }
                    else
                    {
                        key.DeleteArrayElementAtIndex(key.arraySize - 1);
                        value.DeleteArrayElementAtIndex(value.arraySize - 1);
                    }

                    x.Select((x.index - 1).Clamp(0));
                    x.GrabKeyboardFocus();
                },
                onReorderCallbackWithDetails = (ReorderableList list, int oldIndex, int newIndex) =>
                {
                    if (value == null)
                        return;

                    value.MoveArrayElement(oldIndex, newIndex);
                },
                onCanAddCallback = x =>
                {
                    if (key == null)
                        return false;

                    for (int i = 0; i < key.arraySize; i++)
                    {
                        SerializedProperty keyElement = key.GetArrayElementAtIndex(i);
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
                    if (key == null || value == null)
                        return EditorGUIUtility.singleLineHeight;

                    float height = EditorGUI.GetPropertyHeight(key.GetArrayElementAtIndex(i));
                    height = height.Max(EditorGUI.GetPropertyHeight(value.GetArrayElementAtIndex(i)));

                    return height;
                }
            };
        }
    }
}
