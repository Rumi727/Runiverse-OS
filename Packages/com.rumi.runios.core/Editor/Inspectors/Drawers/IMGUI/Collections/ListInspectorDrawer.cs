#nullable enable

using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections
{
    [CustomInspectorDrawer(typeof(IEnumerable), true)]
    [CustomInspectorDrawer(typeof(Array), true, allowInDebug = true)]
    public class ListInspectorDrawer : IMGUIInspectorDrawer
    {
        public ListInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element, rootInspector) { }
        public ListInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList, rootInspector) { }

        public override bool isField => false;

        public ReorderableList? reorderableList { get; private set; }
        
        public bool isExpanded { get; set; } = false;

        readonly AnimFloat animFloat = new AnimFloat(0);
        readonly ConditionalWeakTable<IInspectorListElement, IMGUIInspectorDrawer> elementDrawers = new();

        protected virtual bool IsFixedSize(InspectorFlags flags)
        {
            CheckInspectableList();
            return inspectableList.IsFixedSize ||
                (inspectableList.isArray && (inspectableList.parentElement == null || !inspectableList.parentElement.IsWritable(flags)));
        }

        protected virtual bool CanHeaderResize(Type? elementType, InspectorFlags flags) => CanInsert(elementType, flags);
        
        protected virtual bool CanInsert(Type? elementType, InspectorFlags flags)
        {
            if (elementType == null)
                throw new NullReferenceException($"{nameof(elementType)} is null");
            
            CheckInspectableList();
            if (inspectableList.elementNullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                return true;
            
            return elementType.CanGetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
        }

        protected virtual void OnAddCallback(int index, Type? elementType, InspectorFlags flags)
        {
            CheckInspectableList();
            inspectableList.Insert(index, CreateElementItem(elementType, flags));
        }

        protected virtual object? CreateElementItem(Type? elementType, InspectorFlags flags)
        {
            if (elementType == null)
                throw new NullReferenceException($"{nameof(elementType)} is null");
            
            CheckInspectableList();
            if (inspectableList.elementNullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                return elementType.GetDefaultValue(flags.HasFlagFast(InspectorFlags.NonPublic));
            else
                return elementType.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
        }

        public override void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null)
        {
            CheckInspectableList();
            Type? elementType = inspectableList.inspectionElementType;

            label ??= GUIContent.none;
            
            if (inspectableList.parentElement != null)
            {
                if (NullToggleField(inspectableList.parentElement, position, out position, label, flags))
                    return;
            }
            
            if (inspectableList.instancesIsEmpty)
            {
                EditorGUI.LabelField(position, label, new GUIContent(GetTextOrKey("inspector.no_instance")));   
                return;
            }
            
            SynchronizeCollections();

            bool isFixedSize = IsFixedSize(flags);
            bool canInsert = !isFixedSize && elementType != null && CanInsert(elementType, flags);
            bool canHeaderResize = canInsert && CanHeaderResize(elementType, flags);
            
            reorderableList ??= new ReorderableList(inspectableList, elementType ?? typeof(object), true, false, true, true) { multiSelect = true, };

            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => OnElementGUI(rect, index, isActive, isFocused, flags, clipping);
            reorderableList.elementHeightCallback = index => GetElementHeight(index, flags);
            reorderableList.onCanAddCallback = _ => canInsert;
            reorderableList.onCanRemoveCallback = _ => !isFixedSize;
            
            reorderableList.onAddCallback = x =>
            {
                int index = x.selectedIndices.Any() ? Min(x.selectedIndices.Max() + 1, x.count) : x.count;   
                OnAddCallback(index, elementType, flags);
                x.Select(index);
            };

            reorderableList.onReorderCallbackWithDetails = (_, oldIndex, newIndex) => inspectableList.OnElementMoved(oldIndex, newIndex);
            reorderableList.onChangedCallback = _ => UpdateSourceCollections();
            
            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            EditorGUI.BeginChangeCheck();
            isExpanded = DrawListHeader(position, inspectableList, label, isExpanded, canHeaderResize ? (_ => CreateElementItem(elementType, flags)) : null, isInArray);
            position.y += headHeight + 2;
            if (EditorGUI.EndChangeCheck() && !inspectableList.IsFixedSize)
                UpdateSourceCollections();
            
            position.x += 15 * EditorGUI.indentLevel;
            position.width -= 15 * EditorGUI.indentLevel;
            
            EditorGUI.BeginChangeCheck();
            if (!isInArray)
            {
                if (isExpanded || animFloat.isAnimating)
                    reorderableList.DoList(position);

                if (animFloat.isAnimating)
                    RepaintCurrentWindow();
            }
            else if (isExpanded)
                reorderableList.DoList(position);
            
            if (EditorGUI.EndChangeCheck() && !inspectableList.IsFixedSize)
                UpdateSourceCollections();
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false)
        {
            CheckInspectableList();
            
            if (inspectableList.instancesIsEmpty)
                return EditorGUIUtility.singleLineHeight;
            
            float listHeight = reorderableList?.GetHeight() ?? 0;
            float headHeight = GetYSize(label ?? GUIContent.none, EditorStyles.foldoutHeader);

            if (!isInArray)
            {
                animFloat.target = isExpanded ? listHeight + 2 : 0;
                return headHeight + animFloat.value;
            }
            else
                return headHeight + (isExpanded ? listHeight + 2 : 0);
        }

        public IMGUIInspectorDrawer? GetElementDrawer(int index, InspectorFlags flags)
        {
            CheckInspectableList();
            
            IInspectorListElement? element = inspectableList.GetElement(index, flags);
            if (element == null)
                return null;
            
            if (!elementDrawers.TryGetValue(element, out IMGUIInspectorDrawer? drawer) || drawer.element != element)
            {
                drawer = FindDrawer(element, rootInspector);
                elementDrawers.AddOrUpdate(element, drawer);
            }
            
            return drawer;
        }

        public virtual GUIContent? GetElementLabel(int index) => new GUIContent($"Element {index}");

        public virtual void OnElementGUI(Rect rect, int index, bool isActive, bool isFocused, InspectorFlags flags, Rect? clipping)
        {
            if (EditorGUIUtility.hierarchyMode)
            {
                rect.x += 10;
                rect.width -= 10;
            }
            
            if (EditorGUIUtility.hierarchyMode) BeginLabelWidth(EditorGUIUtility.labelWidth - 31f);
            GetElementDrawer(index, flags)?.OnGUI(rect, GetElementLabel(index), flags, true, clipping);
            if (EditorGUIUtility.hierarchyMode) EndLabelWidth();
        }

        public virtual float GetElementHeight(int index, InspectorFlags flags) => GetElementDrawer(index, flags)?.GetHeight(GetElementLabel(index), flags, true) ?? EditorGUIUtility.singleLineHeight;

        public virtual void SynchronizeCollections()
        {
            CheckInspectableList();
            inspectableList.SynchronizeCollections();
        }

        public virtual void UpdateSourceCollections()
        {
            CheckInspectableList();
            inspectableList.UpdateSourceCollections();
        }
    }
}