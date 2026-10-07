#nullable enable
using RuniOS.Collections.Handlers.Entrys;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Reflection;
using RuniOS.Undos;
using System.Collections;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections
{
    [InspectorDrawer(typeof(IDictionary), true)]
    [InspectorDrawer(typeof(IDictionary<,>), true)]
    public class DictionaryInspectorDrawer : ListInspectorDrawer
    {
        public DictionaryInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }
        public DictionaryInspectorDrawer(IInspectableList inspectableList, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(inspectableList, inheritedAttributes, undoRecorder) { }

        protected override bool IsFixedSize(InspectorFlags flags)
        {
            CheckInspectableDictionary();
            return inspectableDictionary.IsFixedSize;
        }

        protected override bool CanInsert(Type? elementType, InspectorFlags flags)
        {
            CheckInspectableDictionary();

            KeyValuePair<Type, Type>? elementTypePair = inspectableDictionary.inspectionElementType;
            if (elementTypePair == null)
                throw new NullReferenceException($"{nameof(elementTypePair)} is null");

            Type keyType = elementTypePair.Value.Key;
            Type valueType = elementTypePair.Value.Value;

            // 키 타입 인스턴스 생성 가능 여부 체크
            if (!keyType.CanGetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic)))
                return false;

            // 값 타입 Nullable 여부 체크
            if (inspectableDictionary.elementNullabilityInfo?.writeState == NullabilityState.Nullable)
                return true;

            // 값 타입 인스턴스 생성 가능 여부 체크
            return valueType.CanGetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
        }

        protected override object CreateElementItem(Type? elementType, InspectorFlags flags)
        {
            CheckInspectableDictionary();
            if (elementType == null)
                ExceptionUtility.ThrowIfArgumentNull(elementType, nameof(elementType));

            KeyValuePair<Type, Type>? elementTypePair = inspectableDictionary.inspectionElementType;
            if (elementTypePair == null)
                throw new NullReferenceException($"{nameof(elementTypePair)} is null");

            object key = elementTypePair.Value.Key.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
            object? value;
            if (inspectableDictionary.elementNullabilityInfo?.writeState == NullabilityState.Nullable)
                value = elementTypePair.Value.Value.GetDefaultValue(flags.HasFlagFast(InspectorFlags.NonPublic));
            else
                value = elementTypePair.Value.Value.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));

            return EntryHandler.CreateEntry(elementType, key, value);
        }

        public override GUIContent? GetElementLabel(int index) => null;

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
                object? item = inspectableList[i];
                object key = EntryHandler.FindEntry(item).Key ?? nullObject;
                keysBuffer.Add(key);
                
                if (keyCounts.TryGetValue(key, out int currentCount))
                    keyCounts[key] = currentCount + 1;
                else
                    keyCounts[key] = 1;
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

            if (EditorGUIUtility.hierarchyMode)
                rect.xMin += 10;
            
            if (EditorGUIUtility.hierarchyMode) BeginLabelWidth(EditorGUIUtility.labelWidth - 16f);
            GetElementDrawer(index, flags)?.Draw(rect, GetElementLabel(index), flags, new DrawerContext(true, context.clipping));
            if (EditorGUIUtility.hierarchyMode) EndLabelWidth();
        }
    }
}