#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor.IMGUI.Controls;
using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor
{
    public abstract class ExtendedAdvancedDropdown<T> : AdvancedDropdown, IShowableDropdown, ISelectableDropdown<T> where T : AdvancedDropdownItem
    {
        protected ExtendedAdvancedDropdown() : base(new AdvancedDropdownState()) => minimumSize = new Vector2(0, 300);

        public new Vector2 minimumSize
        {
            get => base.minimumSize;
            set => base.minimumSize = value;
        }

        public Vector2 maximumSize
        {
            get => AdvancedDropdownBridge.__GetInstanceFrom(this).maximumSize;
            set => AdvancedDropdownBridge.__GetInstanceFrom(this).maximumSize = value;
        }
        
        public T? selectedItem { get; private set; }
        public event Action<T>? onSelectedItem;
        
        public void DrawLayoutButton(string content, params GUILayoutOption[] options) => DrawLayoutButton(new GUIContent(content), FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public void DrawLayoutButton(GUIContent content, params GUILayoutOption[] options) => DrawLayoutButton(content, FocusType.Keyboard, EditorStyles.miniPullDown, options);

        public void DrawLayoutButton(string content, FocusType focusType, params GUILayoutOption[] options) => DrawLayoutButton(new GUIContent(content), focusType, EditorStyles.miniPullDown, options);
        public void DrawLayoutButton(GUIContent content, FocusType focusType, params GUILayoutOption[] options) => DrawLayoutButton(content, focusType, EditorStyles.miniPullDown, options);

        public void DrawLayoutButton(string content, FocusType focusType, GUIStyle style, params GUILayoutOption[] options) => DrawLayoutButton(new GUIContent(content), focusType, style, options);
        public void DrawLayoutButton(GUIContent content, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            Rect position = EditorGUILayout.GetControlRect(false, GetYSize(style), options);
            DrawButton(position, content, focusType, style);
        }

        public void DrawButton(Rect position, string content) => DrawButton(position, new GUIContent(content), FocusType.Keyboard, EditorStyles.miniPullDown);
        public void DrawButton(Rect position, GUIContent content) => DrawButton(position, content, FocusType.Keyboard, EditorStyles.miniPullDown);

        public void DrawButton(Rect position, string content, FocusType focusType) => DrawButton(position, new GUIContent(content), focusType, EditorStyles.miniPullDown);
        public void DrawButton(Rect position, GUIContent content, FocusType focusType) => DrawButton(position, content, focusType, EditorStyles.miniPullDown);

        public void DrawButton(Rect position, string content, FocusType focusType, GUIStyle style) => DrawButton(position, new GUIContent(content), focusType, style);
        public void DrawButton(Rect position, GUIContent content, FocusType focusType, GUIStyle style)
        {
            if (EditorGUI.DropdownButton(position, content, focusType, style))
            {
                AdvancedDropdownBridge.__GetInstanceFrom(this).m_State = new AdvancedDropdownState();
                Show(position);
            }
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            selectedItem = (T)item;
            onSelectedItem?.Invoke(selectedItem);
        }
    }
}
