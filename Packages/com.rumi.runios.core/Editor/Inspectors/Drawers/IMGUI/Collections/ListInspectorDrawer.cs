#nullable enable

using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Reflection;
using RuniOS.Undos;
using System.Collections;
using System.Runtime.CompilerServices;
using UnityEditor.AnimatedValues;
using UnityEditorInternal;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI.Collections
{
    [InspectorDrawer(typeof(IEnumerable), true)]
    [InspectorDrawer(typeof(Array), true, allowInDebug = true)]
    public class ListInspectorDrawer : IMGUIInspectorDrawer
    {
        public ListInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) { }
        public ListInspectorDrawer(IInspectableList inspectableList, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(inspectableList, inheritedAttributes, undoRecorder) { }

        // 진짜 누가봐도 노치식 코딩이라 이러면 안될 것 같은데 딱히 이거 말곤 방법이 떠오르지 않음.
        public readonly record struct Property(bool draggable, bool displayHeader, bool displayAddButton, bool displayRemoveButton)
        {
            public readonly bool draggable = draggable;
            public readonly bool displayHeader = displayHeader;
            public readonly bool displayAddButton = displayAddButton;
            public readonly bool displayRemoveButton = displayRemoveButton;
        }

        public override bool isField => false;

        public ReorderableList? reorderableList { get; private set; }
        
        public Property property
        {
            get => _property;
            set
            {
                if (_property == value)
                    return;
                
                _property = value;
                reorderableList = null;
            }
        }
        Property _property = new Property(true, false, true, true);

        public bool drawFoldout { get; set; } = true;

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
            if (inspectableList.elementNullabilityInfo?.writeState == NullabilityState.Nullable)
                return true;
            
            return elementType.CanGetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
        }

        protected virtual void OnAddCallback(int index, Type? elementType, InspectorFlags flags)
        {
            CheckInspectableList();
            
            object? newValue = CreateElementItem(elementType, flags);
            inspectableList.Insert(index, newValue);
            
            undoRecorder?.Record
            (
                () => inspectableList.RemoveAt(index),
                () => inspectableList.Insert(index, newValue),
                GetAddElementUndoName(inspectable, variableElement),
                UndoHandler.instance.GetTokenForCurrentUnityGroup()
            );
        }
        
        protected virtual void OnRemoveCallback(int index)
        {
            CheckInspectableList();

            object? lastValue = inspectableList[index]; 
            inspectableList.RemoveAt(index);
            
            undoRecorder?.Record
            (
                () => inspectableList.Insert(index, lastValue),
                () => inspectableList.RemoveAt(index),
                GetRemoveElementUndoName(inspectable, variableElement),
                UndoHandler.instance.GetTokenForCurrentUnityGroup()
            );
        }

        protected virtual void OnReorderCallback(int oldIndex, int newIndex)
        {
            CheckInspectableList();
            inspectableList.OnElementMoved(oldIndex, newIndex);
            
            undoRecorder?.Record
            (
                () => inspectableList.Move(newIndex, oldIndex),
                () => inspectableList.Move(oldIndex, newIndex),
                GetMoveElementUndoName(inspectable, variableElement),
                UndoHandler.instance.GetTokenForCurrentUnityGroup()
            );
        }

        protected virtual object? CreateElementItem(Type? elementType, InspectorFlags flags)
        {
            if (elementType == null)
                throw new NullReferenceException($"{nameof(elementType)} is null");
            
            CheckInspectableList();
            if (inspectableList.elementNullabilityInfo?.writeState == NullabilityState.Nullable)
                return elementType.GetDefaultValue(flags.HasFlagFast(InspectorFlags.NonPublic));
            else
                return elementType.GetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic));
        }

        protected override void OnGUI(Rect position, GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            CheckInspectableList();
            Type? elementType = inspectableList.inspectionElementType;

            label ??= new GUIContent(element?.displayName ?? inspectable.inspectionDisplayName);
            
            if (inspectableList.parentElement != null)
            {
                if (NullToggleField(inspectableList.parentElement, position, out position, label, flags, nullText, undoRecorder))
                    return;
            }
            
            if (inspectableList.instancesIsEmpty)
            {
                EditorGUI.LabelField(position, label, TrTempContent("inspector.no_instance"));
                return;
            }
            
            SynchronizeCollections();

            bool isFixedSize = IsFixedSize(flags);
            bool canInsert = !isFixedSize && elementType != null && CanInsert(elementType, flags);
            bool canHeaderResize = canInsert && CanHeaderResize(elementType, flags);
            
            reorderableList ??= new ReorderableList(inspectableList, elementType ?? typeof(object), property.draggable, property.displayHeader, property.displayAddButton, property.displayRemoveButton) 
            {
                multiSelect = true,
                drawHeaderCallback = rect => GUI.Label(rect, label)
            };
            
            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) => OnElementGUI(rect, index, isActive, isFocused, flags, context);
            reorderableList.elementHeightCallback = index => GetElementHeight(index, flags, context);
            reorderableList.onCanAddCallback = _ => canInsert;
            reorderableList.onCanRemoveCallback = _ => !isFixedSize;
            
            reorderableList.onAddCallback = x =>
            {
                int index = x.selectedIndices.Any() ? Min(x.selectedIndices.Max() + 1, x.count) : x.count;   
                OnAddCallback(index, elementType, flags);
                x.Select(index);
            };

            reorderableList.onRemoveCallback = x =>
            {
                if (x.selectedIndices.Count > 0)
                {
                    foreach (var index in x.selectedIndices.OrderByDescending(i => i))
                        OnRemoveCallback(index);
                    
                    x.Select((x.selectedIndices.Min() - 1).Clamp(0));
                }
                else
                {
                    int count = x.count;
                    OnRemoveCallback(count - 1);
                    x.Select(count - 2);
                }
            };

            reorderableList.onReorderCallbackWithDetails = (_, oldIndex, newIndex) => OnReorderCallback(oldIndex, newIndex);
            
            float headHeight = GetYSize(label, EditorStyles.foldoutHeader);
            position.height = headHeight;

            if (drawFoldout)
            {
                isExpanded = DrawListHeader(position, inspectableList, label, isExpanded, canHeaderResize ? (_ => CreateElementItem(elementType, flags)) : null, context.isInArray);
                position.y += headHeight + 2;

                position.x += 15 * EditorGUI.indentLevel;
                position.width -= 15 * EditorGUI.indentLevel;

                if (!context.isInArray)
                {
                    if (isExpanded || animFloat.isAnimating)
                        reorderableList.DoList(position);

                    if (animFloat.isAnimating)
                        RepaintCurrentWindow();
                }
                else if (isExpanded)
                    reorderableList.DoList(position);
            }
            else
                reorderableList.DoList(position);
        }

        public override float GetHeight(GUIContent? label, InspectorFlags flags, DrawerContext context = default)
        {
            CheckInspectableList();
            
            if (inspectableList.instancesIsEmpty)
                return EditorGUIUtility.singleLineHeight;
            
            float listHeight = reorderableList?.GetHeight() ?? 0;
            
            if (drawFoldout)
            {
                float headHeight = GetYSize(label ?? GUIContent.none, EditorStyles.foldoutHeader);

                if (!context.isInArray)
                {
                    animFloat.target = isExpanded ? listHeight + 2 : 0;
                    return headHeight + animFloat.value;
                }
                else
                    return headHeight + (isExpanded ? listHeight + 2 : 0);
            }
            else
                return listHeight;
        }

        public IMGUIInspectorDrawer? GetElementDrawer(int index, InspectorFlags flags)
        {
            CheckInspectableList();
            
            IInspectorListElement? element = inspectableList.GetElement(index, flags);
            if (element == null)
                return null;
            
            if (!elementDrawers.TryGetValue(element, out IMGUIInspectorDrawer? drawer) || drawer.element != element)
            {
                drawer = FindDrawer(element, attributes.Where(x => !x.applyToSelf), undoRecorder);
                elementDrawers.AddOrUpdate(element, drawer);
            }
            
            return drawer;
        }

        public virtual GUIContent? GetElementLabel(int index) => new GUIContent($"Element {index}");

        public virtual void OnElementGUI(Rect rect, int index, bool isActive, bool isFocused, InspectorFlags flags, DrawerContext context = default)
        {
            if (EditorGUIUtility.hierarchyMode)
                rect.xMin += 10;
            
            if (EditorGUIUtility.hierarchyMode) BeginLabelWidth(EditorGUIUtility.labelWidth - 31f);
            GetElementDrawer(index, flags)?.Draw(rect, GetElementLabel(index), flags, context.InArray());
            if (EditorGUIUtility.hierarchyMode) EndLabelWidth();
        }

        public virtual float GetElementHeight(int index, InspectorFlags flags, DrawerContext context = default) => GetElementDrawer(index, flags)?.GetHeight(GetElementLabel(index), flags, context.InArray() ) ?? EditorGUIUtility.singleLineHeight;

        public virtual void SynchronizeCollections()
        {
            CheckInspectableList();
            inspectableList.SynchronizeCollections();
        }
    }
}