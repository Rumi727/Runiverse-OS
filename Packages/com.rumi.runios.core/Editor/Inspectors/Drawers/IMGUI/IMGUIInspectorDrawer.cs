#nullable enable
using RuniOS.Editor.Inspectors.Attributes.IMGUI;
using RuniOS.Inspectors;
using RuniOS.Inspectors.Attributes;
using RuniOS.Inspectors.Drawers;
using RuniOS.Linq;
using RuniOS.Reflection;
using RuniOS.Undos;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public abstract class IMGUIInspectorDrawer : InspectorDrawer
    {
        static readonly object?[] args = new object?[2];
        [return: NotNullIfNotNull(nameof(element))]
        public static IMGUIInspectorDrawer? FindDrawer(IInspectorVariableElement? element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null, Func<(Type type, CustomInspectorDrawerAttribute attribute), bool>? predicate = null)
        {
            if (element == null)
                return null;

            Type? type = AttributeTypeResolver<IMGUIInspectorDrawer, CustomInspectorDrawerAttribute>.FindDrawerType(element.variableType, predicate);
            if (type == null)
                return new ObjectInspectorDrawer(element, inheritedAttributes, undoRecorder);

            args[0] = element;
            args[1] = inheritedAttributes;
            args[2] = undoRecorder;

            return (IMGUIInspectorDrawer)Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, args, null);
        }

        public abstract bool isField { get; }

        public string? nullText { get; set; } = null;

        public ImmutableArray<IMGUIInspectorAttributeDrawer> attributeDrawers { get; }

        int currentAttributeIndex = -1;
        
        /// <summary>
        /// UI 요소를 렌더링합니다.
        /// </summary>
        public void Draw(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null)
        {
            currentAttributeIndex++;
            
            int index = currentAttributeIndex;
            if (index >= attributeDrawers.Length)
            {
                currentAttributeIndex = -1;

                OnGUI(position, label, flags, isInArray, clipping);
                return;
            }

            try
            {
                attributeDrawers[index].OnGUI(this, position, label, flags, isInArray, clipping);
            }
            finally
            {
                if (index == currentAttributeIndex)
                    currentAttributeIndex = -1;
            }
        }

        protected abstract void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null);

        public virtual float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => EditorGUIUtility.singleLineHeight;

        protected static string GetVariableUndoName(IInspectorVariableElement variableElement) => GetUndoName("undo.modify.property_in_object", variableElement.inspectable, variableElement.path);

        protected static string GetAddElementUndoName(IInspectable inspectable, IInspectorVariableElement? variableElement = null) => GetUndoName("undo.collection.add", inspectable, variableElement?.path ?? GetTextOrKey("gui.collection"));

        protected static string GetRemoveElementUndoName(IInspectable inspectable, IInspectorVariableElement? variableElement = null) => GetUndoName("undo.collection.remove", inspectable, variableElement?.path ?? GetTextOrKey("gui.collection"));
        
        protected static string GetMoveElementUndoName(IInspectable inspectable, IInspectorVariableElement? variableElement = null) => GetUndoName("undo.collection.move", inspectable, variableElement?.path ?? GetTextOrKey("gui.collection"));
        
        static string GetUndoName(string key, IInspectable inspectable, string path)
        {
            var rootInspectable = inspectable;
            for (; rootInspectable.parentElement != null; rootInspectable = rootInspectable.parentElement.inspectable) { }

            string name = GetTextOrKey(key);
            name = new PlaceholderReplacePair("object", rootInspectable.inspectionDisplayName).ReplaceAsPlaceholder(name);
            name = new PlaceholderReplacePair("property", path).ReplaceAsPlaceholder(name);

            return name;
        }

        /// <returns>변수가 null 값인지 여부를 반환합니다</returns>
        protected static bool NullToggleField(IInspectorVariableElement variableElement, Rect position, out Rect resultPosition, GUIContent? label, InspectorFlags flags, string? nullText = null, IUndoRecorder? undoRecorder = null)
        {
            resultPosition = position;

            if (variableElement.variableType.IsValueType)
                return false;

            return NullToggleField
            (
                position,
                out resultPosition,
                label,
                variableElement.IsReadable(flags) ? !variableElement.value.IsNull() : null,
                variableElement.IsWritable(flags) ? (x =>
                {
                    if (x)
                    {
                        object value = variableElement.variableType.GetDefaultValueNotNull();
                        variableElement.value = value;
                        
                        IInspectorVariableElement clonedElement = variableElement.Clone();
                        undoRecorder?.Record
                        (
                            () => clonedElement.value = null,
                            () => clonedElement.value = value,
                            GetVariableUndoName(clonedElement),
                            UndoHandler.instance.GetTokenForCurrentUnityGroup(),
                            clonedElement.path
                        );
                    }
                    else
                    {
                        object? undoValue = variableElement.GetValueOrDefault(flags);
                        variableElement.value = null;

                        IInspectorVariableElement clonedElement = variableElement.Clone();
                        undoRecorder?.Record
                        (
                            () => clonedElement.value = undoValue,
                            () => clonedElement.value = null,
                            GetVariableUndoName(clonedElement),
                            UndoHandler.instance.GetTokenForCurrentUnityGroup(),
                            clonedElement.path
                        );
                    }
                }) : null,
                variableElement.variableType.CanGetDefaultValueNotNull(flags.HasFlagFast(InspectorFlags.NonPublic)),
                variableElement.nullabilityInfo?.writeState,
                nullText ?? $"null ({variableElement.variableType.GetTypeDisplayName()})"
            );
        }

        /// <returns>변수가 null 값인지 여부를 반환합니다</returns>
        protected static bool NullToggleField(Rect position, out Rect resultPosition, GUIContent? label, bool? hasValue, Action<bool>? writeAction, bool isInstanceCreatable, NullabilityState? nullabilityState, string nullText)
        {
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (position.width - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            using (new EditorGUI.DisabledScope(writeAction == null))
            {
                if (hasValue != null)
                {
                    if (!hasValue.Value || nullabilityState == NullabilityState.Nullable)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.BeginDisabledGroup(!hasValue.Value && !isInstanceCreatable);

                        BeginIndentLevel(0);
                        bool toggleValue = EditorGUI.Toggle(toggleRect, hasValue.Value);
                        EndIndentLevel();

                        EditorGUI.EndDisabledGroup();
                        if (EditorGUI.EndChangeCheck())
                            writeAction?.Invoke(toggleValue);

                        position.width -= toggleRect.width + 4;
                    }

                    resultPosition = position;

                    if (!hasValue.Value)
                    {
                        GUI.enabled = true;

                        position.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.LabelField(position, label ?? GUIContent.none, new GUIContent(nullText));

                        return true;
                    }
                }
                else
                {
                    BeginIndentLevel(0);

                    if (nullabilityState == NullabilityState.Nullable)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.Toggle(toggleRect, false);
                        if (EditorGUI.EndChangeCheck())
                            writeAction?.Invoke(false);

                        toggleRect.x -= toggleRect.width + 2;
                        position.width -= toggleRect.width + 2;
                    }

                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.BeginDisabledGroup(!isInstanceCreatable);

                        EditorGUI.Toggle(toggleRect, true);

                        EditorGUI.EndDisabledGroup();
                        if (EditorGUI.EndChangeCheck())
                            writeAction?.Invoke(true);

                        position.width -= toggleRect.width + 2;
                    }

                    EndIndentLevel();
                }
            }

            resultPosition = position;
            return false;
        }

        protected IMGUIInspectorDrawer(IInspectorVariableElement element, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(element, inheritedAttributes, undoRecorder) =>
            attributeDrawers = attributes.Select(x => IMGUIInspectorAttributeDrawer.FindDrawer(x)).WhereNotNull().ToImmutableArray();
        
        protected IMGUIInspectorDrawer(IInspectableList inspectableList, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(inspectableList, inheritedAttributes, undoRecorder) =>
            attributeDrawers = attributes.Select(x => IMGUIInspectorAttributeDrawer.FindDrawer(x)).WhereNotNull().ToImmutableArray();
        
        protected IMGUIInspectorDrawer(IInspectableDictionary inspectableDictionary, IEnumerable<IInspectorAttribute> inheritedAttributes, IUndoRecorder? undoRecorder = null) : base(inspectableDictionary, inheritedAttributes, undoRecorder) =>
            attributeDrawers = attributes.Select(x => IMGUIInspectorAttributeDrawer.FindDrawer(x)).WhereNotNull().ToImmutableArray();
    }
}