#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Undos;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections.Generic
{
    [InspectorDrawer(typeof(ISet<>), true)]
    public class SetInspectorDrawer : ListInspectorDrawer
    {
        public SetInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }
        public SetInspectorDrawer(IInspectableList inspectableList, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(inspectableList, inheritedAttributes, undoRecorder) { }

        readonly HashSet<int> duplicatedIndexes = new();
        readonly List<object?> keysBuffer = new();
        readonly Dictionary<object, int> keyCounts = new();
        readonly object nullObject = new object();
        public override void SynchronizeCollections()
        {
            CheckInspectableList();
            base.SynchronizeCollections();

            duplicatedIndexes.Clear();
            keysBuffer.Clear();
            keyCounts.Clear();

            for (int i = 0; i < inspectableList.Count; i++)
            {
                object item = inspectableList[i] ?? nullObject;
                keysBuffer.Add(item);
                
                if (keyCounts.TryGetValue(item, out int currentCount))
                    keyCounts[item] = currentCount + 1;
                else
                    keyCounts[item] = 1;
            }

            for (int i = 0; i < keysBuffer.Count; i++)
            {
                object? key = keysBuffer[i];
                if (key != null && keyCounts.TryGetValue(key, out int count) && count > 1)
                    duplicatedIndexes.Add(i);
            }
        }

        public override void OnElementGUI(Rect rect, int index, bool isActive, bool isFocused, InspectorFlags flags, DrawerContext context = default)
        {
            if (duplicatedIndexes.Contains(index))
            {
                Rect iconRect = rect;
                iconRect.x -= 6;
                iconRect.width = 20;
                iconRect.height = EditorGUIUtility.singleLineHeight;
                
                if (!EditorGUIUtility.hierarchyMode)
                    rect.xMin += iconRect.width - 10;

                GUIContent content = new GUIContent(EditorGUIUtility.IconContent("console.warnicon.sml")) { tooltip = GetTextOrKey("inspector.invalid.collection.duplicate_key") };
                GUI.Label(iconRect, content);
            }

            base.OnElementGUI(rect, index, isActive, isFocused, flags, context);
        }
    }
}