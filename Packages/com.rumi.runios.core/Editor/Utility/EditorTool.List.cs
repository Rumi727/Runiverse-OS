#nullable enable
using RuniOS.Collections.Generic;
using System;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor
{
    public partial class EditorTool
    {
        public delegate void ListHeaderAddAction(IList list, int index);
        public delegate void ListHeaderRemoveAction(IList list, int index);

        public static bool DrawListHeaderLayout(IList? list, string label, bool isExpanded, bool isInArray = false) => DrawListHeaderLayout(list, new GUIContent(label), isExpanded, null, isInArray);
        public static bool DrawListHeaderLayout(IList? list, GUIContent label, bool isExpanded, bool isInArray = false) => DrawListHeaderLayout(list, label, isExpanded, null, isInArray);
        public static bool DrawListHeaderLayout(IList? list, string label, bool isExpanded, Func<int, object?>? activator, bool isInArray = false) => DrawListHeaderLayout(list, new GUIContent(label), isExpanded, activator, isInArray);
        public static bool DrawListHeaderLayout(IList? list, GUIContent label, bool isExpanded, Func<int, object?>? activator, bool isInArray = false) => DrawListHeader(EditorGUILayout.GetControlRect(false, GetYSize(EditorStyles.foldoutHeader)), list, label, isExpanded, activator, isInArray);

        public static bool DrawListHeader(Rect position, IList? list, string label, bool isExpanded, bool isInArray = false) => DrawListHeader(position, list, new GUIContent(label), isExpanded, null, isInArray);
        public static bool DrawListHeader(Rect position, IList? list, GUIContent label, bool isExpanded, bool isInArray = false) => DrawListHeader(position, list, label, isExpanded, null, isInArray);
        public static bool DrawListHeader(Rect position, IList? list, string label, bool isExpanded, Func<int, object?>? activator, bool isInArray = false) => DrawListHeader(position, list, new GUIContent(label), isExpanded, activator, isInArray);
        public static bool DrawListHeader(Rect position, IList? list, GUIContent label, bool isExpanded, Func<int, object?>? activator, bool isInArray = false)
        {
            position.x += EditorGUI.indentLevel * 15;
            
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
                countPosition.x += countPosition.width - 48 - (EditorGUI.indentLevel * 15);
                countPosition.width = 48;

                if (list == null)
                    return isExpanded;

                bool isAllGeneric = activator != null || CollectionGenericUtility.GetEnumerableElementType(list.GetType()) != null;
                bool isFixedSize = list.IsFixedSize || !isAllGeneric;

                EditorGUI.BeginChangeCheck();

                int firstCount = list.Count;
                
                EditorGUI.BeginDisabledGroup(isFixedSize);
                int count = EditorGUI.DelayedIntField(countPosition, firstCount);
                EditorGUI.EndDisabledGroup();

                if (EditorGUI.EndChangeCheck())
                    list.Resize(count, activator);
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
