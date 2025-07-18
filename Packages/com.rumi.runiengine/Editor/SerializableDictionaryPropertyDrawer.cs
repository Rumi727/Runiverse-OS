#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

using EditorGUI = UnityEditor.EditorGUI;
using EditorGUIUtility = UnityEditor.EditorGUIUtility;

namespace RuniEngine.Editor.Drawers
{
    [CustomPropertyDrawer(typeof(ISerializableDictionary), true)]
    sealed class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        readonly Dictionary<string, ReorderableList> reorderableLists = new Dictionary<string, ReorderableList>();

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GetListProperty(property, out SerializedObject serializedObject, out SerializedProperty? key, out SerializedProperty? value);
            if (key == null || value == null)
            {
#pragma warning disable IDE0079 // 불필요한 비표시 오류(Suppression) 제거
#pragma warning disable UNT0027 // Do not call PropertyDrawer.OnGUI()
                base.OnGUI(position, property, label);
#pragma warning restore UNT0027 // Do not call PropertyDrawer.OnGUI()
#pragma warning restore IDE0079 // 불필요한 비표시 오류(Suppression) 제거
                return;
            }

            bool isInArray = IsInArray(property);

            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            ListHeader(position, key, label, index =>
            {
                key.InsertArrayElementAtIndex(index);
                value.InsertArrayElementAtIndex(index);

                //InsertArrayElementAtIndex 함수는 값을 복제하기 때문에 키를 기본값으로 정해줘야 제대로 생성할 수 있게 됨
                SetDefaultValue(key.GetArrayElementAtIndex(index));
            }, index =>
            {
                key.DeleteArrayElementAtIndex(index);
                value.DeleteArrayElementAtIndex(index);
            });

            position.y += headHeight + 2;

            if (key.isExpanded)
            {
                ReorderableList reorderableList = GetReorderableList(serializedObject, property, key, value);
                reorderableList.DoList(position);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GetListProperty(property, out SerializedObject serializedObject, out SerializedProperty? key, out SerializedProperty? value);
            if (key == null || value == null)
                return base.GetPropertyHeight(property, label);

            float headerHeight = GetYSize(label, EditorStyles.foldoutHeader);
            float height;
            ReorderableList reorderableList = GetReorderableList(serializedObject, property, key, value);
            if (key.isExpanded)
                height = reorderableList.GetHeight() + 2;
            else
                height = 0;

            return height + headerHeight;
        }

        public static void GetListProperty(SerializedProperty property, out SerializedObject serializedObject, out SerializedProperty? key, out SerializedProperty? value)
        {
            serializedObject = property.serializedObject;

            key = property.FindPropertyRelative(nameof(ISerializableDictionary.serializableKeys));
            value = property.FindPropertyRelative(nameof(ISerializableDictionary.serializableValues));
        }

        public ReorderableList GetReorderableList(SerializedObject serializedObject, SerializedProperty property, SerializedProperty? key, SerializedProperty? value)
        {
            if (reorderableLists.TryGetValue(property.propertyPath, out ReorderableList result))
                return result;
            else
            {
                return reorderableLists[property.propertyPath] = new ReorderableList(serializedObject, key)
                {
                    multiSelect = true,
                    headerHeight = 0,
                    drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                    {
                        if (key == null || value == null)
                            return;

                        string keyLabel = "Key";
                        string valueLabel = "Value";

                        rect.width /= 2;
                        rect.width -= 10;

                        BeginLabelWidth(keyLabel);

                        SerializedProperty keyElement = key.GetArrayElementAtIndex(index);
                        object? lastValue = keyElement.boxedValue;

                        rect.height = EditorGUI.GetPropertyHeight(keyElement);

                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(rect, keyElement, new GUIContent(keyLabel), IsChildrenIncluded(keyElement));

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

                        EditorGUI.PropertyField(rect, valueElement, new GUIContent(valueLabel), IsChildrenIncluded(valueElement));

                        EndLabelWidth();
                    },
                    onAddCallback = x =>
                    {
                        if (key == null || value == null)
                            return;

                        int index = key.arraySize;

                        key.InsertArrayElementAtIndex(index);
                        value.InsertArrayElementAtIndex(index);

                        //InsertArrayElementAtIndex 함수는 값을 복제하기 때문에 키를 기본값으로 정해줘야 제대로 생성할 수 있게 됨
                        SetDefaultValue(key.GetArrayElementAtIndex(index));

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

                        x.Select(Mathf.Max(x.index - 1, 0));
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
                                if (boxedValue == GetDefaultValue(boxedValue.GetType()))
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
                        height = Mathf.Max(height, EditorGUI.GetPropertyHeight(value.GetArrayElementAtIndex(i)));

                        return height;
                    }
                };
            }
        }

        public static void ListHeader(Rect position, SerializedProperty property, GUIContent label) => ListHeader(position, property, label, null, null);
        public static void ListHeader(Rect position, SerializedProperty property, string label, Action<int>? addAction, Action<int>? removeAction) => ListHeader(position, property, new GUIContent(label), addAction, removeAction);
        public static void ListHeader(Rect position, SerializedProperty property, GUIContent label, Action<int>? addAction, Action<int>? removeAction)
        {
            bool isInArray = IsInArray(property);

            {
                Rect headerPosition = position;
                headerPosition.width -= 48;

                EditorGUI.BeginProperty(headerPosition, label, property);
                EditorGUI.showMixedValue = false;

                if (!isInArray)
                {
                    property.isExpanded = EditorGUI.BeginFoldoutHeaderGroup(headerPosition, property.isExpanded, label);
                    EditorGUI.EndFoldoutHeaderGroup();
                }
                else
                    property.isExpanded = EditorGUI.Foldout(headerPosition, property.isExpanded, label, true);

                EditorGUI.EndProperty();
            }

            {
                Rect countPosition = position;
                countPosition.x += countPosition.width - 48;
                countPosition.width = 48;

                int count = EditorGUI.DelayedIntField(countPosition, property.arraySize);
                int addCount = count - property.arraySize;
                if (addCount > 0)
                {
                    for (int i = 0; i < addCount; i++)
                    {
                        int index = property.arraySize;
                        if (addAction != null)
                            addAction(index);
                        else
                            property.InsertArrayElementAtIndex(index);
                    }
                }
                else
                {
                    addCount = -addCount;
                    for (int i = 0; i < addCount; i++)
                    {
                        int index = property.arraySize - 1;
                        if (removeAction != null)
                            removeAction(index);
                        else
                            property.DeleteArrayElementAtIndex(index);
                    }
                }
            }
        }

        public static SerializedProperty? GetParent(SerializedProperty serializedProperty)
        {
            string path = serializedProperty.propertyPath;
            if (path.Contains('.'))
            {
                int index = path.LastIndexOf('.');
                path = path.Substring(0, index);

                return serializedProperty.serializedObject.FindProperty(path);
            }

            return null;
        }

        public static bool IsInArray(SerializedProperty? serializedProperty)
        {
            if (serializedProperty == null)
                return false;

            while ((serializedProperty = GetParent(serializedProperty)) != null)
            {
                if (serializedProperty.isArray)
                    return true;
            }

            return false;
        }

        public static object? GetDefaultValue(Type type)
        {
            if (!type.IsValueType)
                return null;

            return Activator.CreateInstance(type);
        }

        public static void SetDefaultValue(SerializedProperty serializedProperty)
        {
            if (serializedProperty.isArray)
            {
                serializedProperty.ClearArray();
                return;
            }

            if (serializedProperty.propertyType == SerializedPropertyType.String)
            {
                serializedProperty.stringValue = string.Empty;
                return;
            }

            if (serializedProperty.boxedValue != null)
            {
                serializedProperty.boxedValue = GetDefaultValue(serializedProperty.boxedValue.GetType());
                return;
            }
        }

        public static float GetXSize(GUIContent content, GUIStyle style) => style.CalcSize(content).x;
        public static float GetYSize(GUIContent content, GUIStyle style) => style.CalcSize(content).y;

        public static void BeginLabelWidth(string label) => BeginLabelWidth(GetXSize(new GUIContent(label), EditorStyles.label) + 2);

        static readonly Stack<float> labelWidthQueue = new Stack<float>();
        public static void BeginLabelWidth(float width)
        {
            if (labelWidthQueue.Count > 0)
                labelWidthQueue.Push(EditorGUIUtility.labelWidth);
            else
                labelWidthQueue.Push(0);

            EditorGUIUtility.labelWidth = width;
        }

        public static void EndLabelWidth()
        {
            if (labelWidthQueue.TryPop(out float result))
                EditorGUIUtility.labelWidth = result;
            else
                EditorGUIUtility.labelWidth = 0;
        }

        public static bool IsChildrenIncluded(SerializedProperty prop) => prop.propertyType == SerializedPropertyType.Generic || prop.propertyType == SerializedPropertyType.Vector4;
    }
}
