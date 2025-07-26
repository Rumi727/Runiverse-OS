#nullable enable
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;
using UnityEngine;

using static RuniEngine.Editor.EditorTool;
using EditorGUI = UnityEditor.EditorGUI;

namespace RuniEngine.Editor
{
    public sealed class AnimatedReorderableList
    {
        public AnimatedReorderableList(SerializedProperty property)
        {
            this.property = property;

            float height;
            reorderableList = CreateReorderableList();
            if (property.isExpanded)
                height = reorderableList.GetHeight();
            else
                height = 0;

            animFloat = new AnimFloat(height);
        }

        public SerializedProperty property { get; }

        public AnimFloat? animFloat { get; } = null;
        public ReorderableList? reorderableList { get; } = null;

        public void Draw(Rect position) => Draw(position, null);
        public void Draw(Rect position, GUIContent? label) => OnGUI(position, label);

        public void DrawLayout(GUIContent? label = null)
        {
            Rect position = EditorGUILayout.GetControlRect(false, GetPropertyHeight(label));
            OnGUI(position, label);
        }

        void OnGUI(Rect position, GUIContent? label)
        {
            if (reorderableList == null)
                return;

            label ??= new GUIContent(property.displayName);

            bool isInArray = property.IsInArray();

            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            ListHeader(position, property, label);
            position.y += headHeight + 2;

            if (!isInArray && animFloat != null)
            {
                if (property.isExpanded || animFloat.isAnimating)
                {
                    if (animFloat.isAnimating)
                        GUI.BeginClip(new Rect(0, 0, position.x + position.width, position.y + animFloat.value));

                    reorderableList.DoList(position);

                    if (animFloat.isAnimating)
                        GUI.EndClip();
                }

                if (animFloat.isAnimating)
                    RepaintCurrentWindow();
            }
            else if (property.isExpanded)
                reorderableList.DoList(position);
        }

        public float GetPropertyHeight(GUIContent? label)
        {
            if (reorderableList == null)
                return 0;

            label ??= new GUIContent(property.displayName);

            float headerHeight = GetYSize(label, EditorStyles.foldoutHeader);
            float height;
            if (property.isExpanded)
                height = reorderableList.GetHeight() + 2;
            else
                height = 0;

            if (animFloat != null)
                animFloat.target = height;

            if (!property.IsInArray())
                return (animFloat?.value ?? 0) + headerHeight;
            else
                return height + headerHeight;
        }

        ReorderableList CreateReorderableList() => new ReorderableList(property.serializedObject, property, true, false, true, true)
        {
            multiSelect = true,
            drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                rect.x += 8;
                rect.width -= 8;
                rect.y += 1;
                rect.height -= 2;

                BeginMinLabelWidth(0, rect.width + 11, 0);

                SerializedProperty element = property.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, element, element.IsGeneric());

                EndLabelWidth();
            },
            elementHeightCallback = i => EditorGUI.GetPropertyHeight(property.GetArrayElementAtIndex(i))
        };
    }
}
