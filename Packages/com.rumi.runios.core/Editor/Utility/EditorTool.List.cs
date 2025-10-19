#nullable enable
using RuniOS.Collections.Generic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public delegate void ListHeaderAddAction(IList list, int index);
        public delegate void ListHeaderRemoveAction(IList list, int index);

        public static bool DrawListHeaderLayout(IEnumerable<IList>? lists, string label, bool isExpanded, bool isInArray = false) => DrawListHeaderLayout(lists, new GUIContent(label), isExpanded, null, null, isInArray);
        public static bool DrawListHeaderLayout(IEnumerable<IList>? lists, GUIContent label, bool isExpanded, bool isInArray = false) => DrawListHeaderLayout(lists, label, isExpanded, null, null, isInArray);
        public static bool DrawListHeaderLayout(IEnumerable<IList>? lists, string label, bool isExpanded, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false) => DrawListHeaderLayout(lists, new GUIContent(label), isExpanded, addAction, removeAction, isInArray);
        public static bool DrawListHeaderLayout(IEnumerable<IList>? lists, GUIContent label, bool isExpanded, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false) => DrawListHeader(EditorGUILayout.GetControlRect(false, GetYSize(EditorStyles.foldoutHeader)), lists, label, isExpanded, addAction, removeAction, isInArray);

        public static bool DrawListHeader(Rect position, IEnumerable<IList>? lists, string label, bool isExpanded, bool isInArray = false) => DrawListHeader(position, lists, new GUIContent(label), isExpanded, null, null, isInArray);
        public static bool DrawListHeader(Rect position, IEnumerable<IList>? lists, GUIContent label, bool isExpanded, bool isInArray = false) => DrawListHeader(position, lists, label, isExpanded, null, null, isInArray);
        public static bool DrawListHeader(Rect position, IEnumerable<IList>? lists, string label, bool isExpanded, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false) => DrawListHeader(position, lists, new GUIContent(label), isExpanded, addAction, removeAction, isInArray);
        public static bool DrawListHeader(Rect position, IEnumerable<IList>? lists, GUIContent label, bool isExpanded, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false)
        {
            {
                Rect headerPosition = position;
                headerPosition.width -= 48;

                if (!isInArray)
                {
                    isExpanded = EditorGUI.BeginFoldoutHeaderGroup(headerPosition, isExpanded, label);
                    EditorGUI.EndFoldoutHeaderGroup();
                }
                else
                    isExpanded = EditorGUI.Foldout(headerPosition, isExpanded, label, true);
            }

            {
                Rect countPosition = position;
                countPosition.x += countPosition.width - 48;
                countPosition.width = 48;

                if (lists == null)
                    return isExpanded;

                EditorGUI.BeginChangeCheck();

                int firstCount = lists.FirstOrDefault()?.Count ?? 0;
                EditorGUI.showMixedValue = lists.Any(x => firstCount != x.Count);

                int count = EditorGUI.DelayedIntField(countPosition, firstCount);

                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var list in lists)
                    {
                        int addCount = count - list.Count;
                        if (addCount > 0)
                        {
                            for (int j = 0; j < addCount; j++)
                            {
                                int index = list.Count;
                                if (addAction != null)
                                    addAction(list, index);
                                else
                                    list.Add(list.GetElementType().GetDefaultValue());
                            }
                        }
                        else
                        {
                            addCount = -addCount;
                            for (int j = 0; j < addCount; j++)
                            {
                                int index = list.Count - 1;
                                if (removeAction != null)
                                    removeAction(list, index);
                                else
                                    list.RemoveAt(index);
                            }
                        }
                    }
                }
            }

            return isExpanded;
        }

        public static void DrawListHeaderLayout(SerializedProperty property, string label) => DrawListHeaderLayout(property, new GUIContent(label), null, null);
        public static void DrawListHeaderLayout(SerializedProperty property, GUIContent label) => DrawListHeaderLayout(property, label, null, null);
        public static void DrawListHeaderLayout(SerializedProperty property, string label, Action<int>? addAction, Action<int>? removeAction) => DrawListHeaderLayout(property, new GUIContent(label), addAction, removeAction);
        public static void DrawListHeaderLayout(SerializedProperty property, GUIContent label, Action<int>? addAction, Action<int>? removeAction)
        {
            float height;
            if (property.IsInArray())
                height = GetYSize(EditorStyles.foldout);
            else
                height = GetYSize(EditorStyles.foldoutHeader);

            DrawListHeader(EditorGUILayout.GetControlRect(false, height), property, label, addAction, removeAction);
        }

        public static void DrawListHeader(Rect position, SerializedProperty property, string label) => DrawListHeader(position, property, new GUIContent(label), null, null);
        public static void DrawListHeader(Rect position, SerializedProperty property, GUIContent label) => DrawListHeader(position, property, label, null, null);
        public static void DrawListHeader(Rect position, SerializedProperty property, string label, Action<int>? addAction, Action<int>? removeAction) => DrawListHeader(position, property, new GUIContent(label), addAction, removeAction);
        public static void DrawListHeader(Rect position, SerializedProperty property, GUIContent label, Action<int>? addAction, Action<int>? removeAction)
        {
            bool isInArray = property.IsInArray();

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
    }
}
