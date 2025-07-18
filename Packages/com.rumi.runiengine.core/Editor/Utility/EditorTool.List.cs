#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Editor
{
    public partial class EditorTool
    {
        public delegate void ListHeaderAddAction(IList list, int listIndex, int index);
        public delegate void ListHeaderRemoveAction(IList list, int listIndex, int index);

        public static bool ListHeaderLayout(IEnumerable<IList>? lists, string label, bool foldout, bool isInArray = false) => ListHeaderLayout(lists, new GUIContent(label), foldout, null, null, isInArray);
        public static bool ListHeaderLayout(IEnumerable<IList>? lists, GUIContent label, bool foldout, bool isInArray = false) => ListHeaderLayout(lists, label, foldout, null, null, isInArray);
        public static bool ListHeaderLayout(IEnumerable<IList>? lists, string label, bool foldout, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false) => ListHeaderLayout(lists, new GUIContent(label), foldout, addAction, removeAction, isInArray);
        public static bool ListHeaderLayout(IEnumerable<IList>? lists, GUIContent label, bool foldout, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false) => ListHeader(EditorGUILayout.GetControlRect(false, GetYSize(EditorStyles.foldoutHeader)), lists, label, foldout, addAction, removeAction, isInArray);

        public static bool ListHeader(Rect position, IEnumerable<IList>? lists, string label, bool foldout, bool isInArray = false) => ListHeader(position, lists, new GUIContent(label), foldout, null, null, isInArray);
        public static bool ListHeader(Rect position, IEnumerable<IList>? lists, GUIContent label, bool foldout, bool isInArray = false) => ListHeader(position, lists, label, foldout, null, null, isInArray);
        public static bool ListHeader(Rect position, IEnumerable<IList>? lists, string label, bool foldout, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false) => ListHeader(position, lists, new GUIContent(label), foldout, addAction, removeAction, isInArray);
        public static bool ListHeader(Rect position, IEnumerable<IList>? lists, GUIContent label, bool foldout, ListHeaderAddAction? addAction, ListHeaderRemoveAction? removeAction, bool isInArray = false)
        {
            {
                Rect headerPosition = position;
                headerPosition.width -= 48;

                if (!isInArray)
                {
                    foldout = EditorGUI.BeginFoldoutHeaderGroup(headerPosition, foldout, label);
                    EditorGUI.EndFoldoutHeaderGroup();
                }
                else
                    foldout = EditorGUI.Foldout(headerPosition, foldout, label, true);
            }

            {
                Rect countPosition = position;
                countPosition.x += countPosition.width - 48;
                countPosition.width = 48;

                if (lists == null)
                    return foldout;

                EditorGUI.BeginChangeCheck();

                int firstCount = lists.FirstOrDefault()?.Count ?? 0;
                EditorGUI.showMixedValue = lists.Any(x => firstCount != x.Count);

                int count = EditorGUI.DelayedIntField(countPosition, firstCount);

                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    int listIndex = 0;
                    foreach (var list in lists)
                    {
                        int addCount = count - list.Count;
                        if (addCount > 0)
                        {
                            for (int j = 0; j < addCount; j++)
                            {
                                int index = list.Count;
                                if (addAction != null)
                                    addAction(list, listIndex, index);
                                else
                                    list.Add(list.GetListType().GetDefaultValue());
                            }
                        }
                        else
                        {
                            addCount = -addCount;
                            for (int j = 0; j < addCount; j++)
                            {
                                int index = list.Count - 1;
                                if (removeAction != null)
                                    removeAction(list, listIndex, index);
                                else
                                    list.RemoveAt(index);
                            }
                        }

                        listIndex++;
                    }
                }
            }

            return foldout;
        }

        public static void ListHeaderLayout(SerializedProperty property, string label) => ListHeaderLayout(property, new GUIContent(label), null, null);
        public static void ListHeaderLayout(SerializedProperty property, GUIContent label) => ListHeaderLayout(property, label, null, null);
        public static void ListHeaderLayout(SerializedProperty property, string label, Action<int>? addAction, Action<int>? removeAction) => ListHeaderLayout(property, new GUIContent(label), addAction, removeAction);
        public static void ListHeaderLayout(SerializedProperty property, GUIContent label, Action<int>? addAction, Action<int>? removeAction)
        {
            float height;
            if (property.IsInArray())
                height = GetYSize(EditorStyles.foldout);
            else
                height = GetYSize(EditorStyles.foldoutHeader);

            ListHeader(EditorGUILayout.GetControlRect(false, height), property, label, addAction, removeAction);
        }

        public static void ListHeader(Rect position, SerializedProperty property, string label) => ListHeader(position, property, new GUIContent(label), null, null);
        public static void ListHeader(Rect position, SerializedProperty property, GUIContent label) => ListHeader(position, property, label, null, null);
        public static void ListHeader(Rect position, SerializedProperty property, string label, Action<int>? addAction, Action<int>? removeAction) => ListHeader(position, property, new GUIContent(label), addAction, removeAction);
        public static void ListHeader(Rect position, SerializedProperty property, GUIContent label, Action<int>? addAction, Action<int>? removeAction)
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
