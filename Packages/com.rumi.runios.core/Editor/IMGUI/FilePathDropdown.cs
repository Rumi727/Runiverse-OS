#nullable enable

using RuniOS.IO;
using RuniOS.Spans;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.IMGUI
{
    public sealed class FilePathDropdown : ExtendedAdvancedDropdown<FilePathDropdownItem>
    {
        readonly List<FilePath> cachedPaths = new List<FilePath>();
        FilePathDropdownItem? imguiSelectedItem;
        
        public void Rebuild(IEnumerable<FilePath> paths)
        {
            cachedPaths.SyncWithEnumerable(paths.OrderByAlphaNumeric(x => x.value, StringComparer.CurrentCulture));
            BuildRoot();
        }

        public FilePath DrawLayout(FilePath value, params GUILayoutOption[] options) => DrawLayout(value, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public FilePath DrawLayout(FilePath value, FocusType focusType, params GUILayoutOption[] options) => DrawLayout(value, focusType, EditorStyles.miniPullDown, options);
        public FilePath DrawLayout(FilePath value, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            DrawLayoutButton(value, focusType, style, options);

            FilePath result = imguiSelectedItem?.path ?? value;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.path;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }

        public FilePath Draw(Rect position, FilePath value) => Draw(position, value, FocusType.Keyboard, EditorStyles.miniPullDown);
        public FilePath Draw(Rect position, FilePath value, FocusType focusType) => Draw(position, value, focusType, EditorStyles.miniPullDown);
        public FilePath Draw(Rect position, FilePath value, FocusType focusType, GUIStyle style)
        {
            DrawButton(position, value, focusType, style);

            FilePath result = imguiSelectedItem?.path ?? value;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.path;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }

        readonly Dictionary<FilePath, FilePathDropdownItem> buildRootPaths = new();
        protected override AdvancedDropdownItem BuildRoot()
        {
            FilePathDropdownItem root = new FilePathDropdownItem(FilePath.empty,  GetTextOrKey("gui.root"));

            buildRootPaths.Clear();
            for (int i = 0; i < cachedPaths.Count; i++)
            {
                FilePath path = cachedPaths[i];
                if (path.IsEmpty())
                {
                    root.AddChild(new FilePathDropdownItem(FilePath.empty, GetTextOrKey("gui.none")));

                    if (cachedPaths.Count > 1)
                        root.AddSeparator();

                    continue;
                }
                
                FilePath splitAllPath = FilePath.empty;
                foreach (var span in path.value.AsSpan().Split(FilePath.directorySeparatorChar))
                {
                    string splitPath = new string(span);
                    splitAllPath += splitPath;

                    if (buildRootPaths.ContainsKey(splitAllPath))
                        continue;
                    
                    FilePath parentPath = splitAllPath.GetParentPath();

                    FilePathDropdownItem item = new FilePathDropdownItem(splitAllPath, splitAllPath.GetFileName());
                    FilePathDropdownItem parentItem = buildRootPaths.GetValueOrDefault(parentPath, root);

                    if (i + 1 < cachedPaths.Count)
                    {
                        int nextPathIndex = i + 1;
                        if (cachedPaths[nextPathIndex].StartsWith(path))
                            parentItem.AddChild(new FilePathDropdownItem(path, path.GetFileName()));
                    }

                    parentItem.AddChild(item);
                    buildRootPaths.Add(splitAllPath, item);
                }
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
