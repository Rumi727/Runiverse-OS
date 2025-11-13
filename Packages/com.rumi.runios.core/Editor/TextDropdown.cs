#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor
{
    public sealed class TextDropdown : ExtendedAdvancedDropdown<TextDropdownItem>
    {
        readonly List<string> cachedPaths = new List<string>();
        TextDropdownItem? imguiSelectedItem;
        
        public void Rebuild(IEnumerable<string> paths)
        {
            cachedPaths.SyncWithEnumerable(paths.Distinct());
            BuildRoot();
        }

        public string DrawLayout(string value, params GUILayoutOption[] options) => DrawLayout(value, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public string DrawLayout(string value, FocusType focusType, params GUILayoutOption[] options) => DrawLayout(value, focusType, EditorStyles.miniPullDown, options);
        public string DrawLayout(string value, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            DrawLayoutButton(value, focusType, style, options);

            string result = imguiSelectedItem?.value ?? value;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.value;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }

        public string Draw(Rect position, string value) => Draw(position, value, FocusType.Keyboard, EditorStyles.miniPullDown);
        public string Draw(Rect position, string value, FocusType focusType) => Draw(position, value, focusType, EditorStyles.miniPullDown);
        public string Draw(Rect position, string value, FocusType focusType, GUIStyle style)
        {
            DrawButton(position, value, focusType, style);

            string result = imguiSelectedItem?.value ?? value;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.value;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }
        
        protected override AdvancedDropdownItem BuildRoot()
        {
            TextDropdownItem root = new TextDropdownItem(string.Empty,  GetTextOrKey("gui.root"));

            if (cachedPaths.Any(string.IsNullOrEmpty))
            {
                root.AddChild(new TextDropdownItem(string.Empty, GetTextOrKey("gui.none")));
                if (cachedPaths.Count > 1)
                    root.AddSeparator();
            }

            for (int i = 0; i < cachedPaths.Count; i++)
            {
                string name = cachedPaths[i] ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                    root.AddChild(new TextDropdownItem(name, name));
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
