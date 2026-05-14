#nullable enable

using RuniOS.IO;
using RuniOS.Spans;
using UnityEditor.IMGUI.Controls;

namespace RuniOS.Editor.IMGUI
{
    public sealed class RuniPathDropdown : ExtendedAdvancedDropdown<RuniPathDropdownItem>
    {
        readonly List<RuniPath> cachedPaths = new List<RuniPath>();
        RuniPathDropdownItem? imguiSelectedItem;
        
        public void Rebuild(IEnumerable<RuniPath> paths)
        {
            cachedPaths.SyncWithEnumerable(paths.OrderByAlphaNumeric(x => x.value, StringComparer.CurrentCulture));
            BuildRoot();
        }

        public RuniPath DrawLayout(RuniPath value, params GUILayoutOption[] options) => DrawLayout(value, FocusType.Keyboard, EditorStyles.miniPullDown, options);
        public RuniPath DrawLayout(RuniPath value, FocusType focusType, params GUILayoutOption[] options) => DrawLayout(value, focusType, EditorStyles.miniPullDown, options);
        public RuniPath DrawLayout(RuniPath value, FocusType focusType, GUIStyle style, params GUILayoutOption[] options)
        {
            DrawLayoutButton(value, focusType, style, options);

            RuniPath result = imguiSelectedItem?.path ?? value;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.path;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }

        public RuniPath Draw(Rect position, RuniPath value) => Draw(position, value, FocusType.Keyboard, EditorStyles.miniPullDown);
        public RuniPath Draw(Rect position, RuniPath value, FocusType focusType) => Draw(position, value, focusType, EditorStyles.miniPullDown);
        public RuniPath Draw(Rect position, RuniPath value, FocusType focusType, GUIStyle style)
        {
            DrawButton(position, value, focusType, style);

            RuniPath result = imguiSelectedItem?.path ?? value;
            if (imguiSelectedItem != null)
            {
                result = imguiSelectedItem.path;
                imguiSelectedItem = null;

                GUI.changed = true;
            }

            return result;
        }

        readonly Dictionary<RuniPath, RuniPathDropdownItem> buildRootPaths = new();
        protected override AdvancedDropdownItem BuildRoot()
        {
            RuniPathDropdownItem root = new RuniPathDropdownItem(RuniPath.empty,  GetTextOrKey("gui.root"));

            buildRootPaths.Clear();
            for (int i = 0; i < cachedPaths.Count; i++)
            {
                RuniPath path = cachedPaths[i];
                if (path.IsEmpty())
                {
                    root.AddChild(new RuniPathDropdownItem(RuniPath.empty, GetTextOrKey("gui.none")));

                    if (cachedPaths.Count > 1)
                        root.AddSeparator();

                    continue;
                }
                
                RuniPath splitAllPath = RuniPath.empty;
                foreach (var span in path.value.AsSpan().Split(RuniPath.directorySeparatorChar))
                {
                    string splitPath = new string(span);
                    splitAllPath += splitPath;

                    if (buildRootPaths.ContainsKey(splitAllPath))
                        continue;
                    
                    RuniPath parentPath = splitAllPath.GetParentPath();

                    RuniPathDropdownItem item = new RuniPathDropdownItem(splitAllPath, splitAllPath.GetFileName());
                    RuniPathDropdownItem parentItem = buildRootPaths.GetValueOrDefault(parentPath, root);

                    if (i + 1 < cachedPaths.Count)
                    {
                        int nextPathIndex = i + 1;
                        if (cachedPaths[nextPathIndex].StartsWith(path))
                            parentItem.AddChild(new RuniPathDropdownItem(path, path.GetFileName()));
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