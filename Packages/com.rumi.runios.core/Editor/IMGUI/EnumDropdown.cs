#nullable enable

using System;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.IMGUI
{
    public sealed class EnumDropdown : ExtendedAdvancedDropdown<EnumDropdownItem>
    {
        public EnumDropdown(Type enumType)
        {
            this.enumType = enumType;
            BuildRoot();
        }
        
        public Type enumType { get; }
        EnumDropdownItem? imguiSelectedItem;

        public T DrawLayout<T>(T enumValue, params GUILayoutOption[] options) where T : Enum => (T)DrawLayout((Enum)enumValue, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public T DrawLayout<T>(T enumValue, FocusType focusType, params GUILayoutOption[] options) where T : Enum => (T)DrawLayout((Enum)enumValue, focusType, EditorStyles.miniPullDown, options);
        public T DrawLayout<T>(T enumValue, FocusType focusType, GUIStyle style, params GUILayoutOption[] options) where T : Enum => (T)DrawLayout((Enum)enumValue, focusType, style, options);

        public Enum DrawLayout(Enum enumValue, params GUILayoutOption[] options) => DrawLayout(enumValue, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public Enum DrawLayout(Enum enumValue, FocusType focusType, params GUILayoutOption[] options) => DrawLayout(enumValue, focusType, EditorStyles.miniPullDown, options);
        public Enum DrawLayout(Enum enumValue, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            DrawLayoutButton(enumValue.ToString(), focusType, style, options);

            Enum result = enumValue;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.enumValue;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }

        public T Draw<T>(Rect position, T enumValue) where T : Enum => Draw(position, enumValue, FocusType.Keyboard, EditorStyles.miniPullDown);
        public T Draw<T>(Rect position, T enumValue, FocusType focusType) where T : Enum => Draw(position, enumValue, focusType, EditorStyles.miniPullDown);
        public T Draw<T>(Rect position, T enumValue, FocusType focusType, GUIStyle style) where T : Enum => (T)Draw(position, (Enum)enumValue, focusType, style);

        public object Draw(Rect position, Enum enumValue) => Draw(position, enumValue, FocusType.Keyboard, EditorStyles.miniPullDown);
        public object Draw(Rect position, Enum enumValue, FocusType focusType) => Draw(position, enumValue, focusType, EditorStyles.miniPullDown);
        public object Draw(Rect position, Enum enumValue, FocusType focusType, GUIStyle style)
        {
            DrawButton(position, enumValue.ToString(), focusType, style);

            Enum result = enumValue;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.enumValue;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }
        
        protected override AdvancedDropdownItem BuildRoot()
        {
            EnumDropdownItem root = new EnumDropdownItem((Enum)enumType.GetDefaultValueNotNull(),  GetTextOrKey("gui.root"));
            if (!enumType.IsEnum)
                return root;
            
            Array array = enumType.GetEnumValues();
            for (int i = 0; i < array.Length; i++)
            {
                Enum item = (Enum)array.GetValue(i);
                root.AddChild(new EnumDropdownItem(item, item.ToString()));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            imguiSelectedItem = selectedItem;
        }
    }
}
