#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public abstract class IMGUIInspectorDrawer : InspectorDrawer
    {
        [return: NotNullIfNotNull(nameof(element))]
        public static IMGUIInspectorDrawer? FindDrawer(IInspectorVariableElement? element, Inspector? rootInspector = null, Func<(Type type, CustomInspectorDrawerAttribute attribute), bool>? predicate = null)
        {
            if (element == null)
                return null;
            
            Type? type = AttributeDrawer<IMGUIInspectorDrawer, CustomInspectorDrawerAttribute>.FindDrawerType(element.variableType, predicate);
            if (type == null)
                return new ObjectInspectorDrawer(element, rootInspector);
            
            return (IMGUIInspectorDrawer)Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new object?[] { element, rootInspector }, null);
        }
        
        /// <summary>
        /// 루트 인스펙터를 가져옵니다.
        /// </summary>
        public Inspector? rootInspector { get; }

        public abstract bool isField { get; }
        
        public string? nullText { get; set; } = null;
        
        /// <summary>
        /// UI 요소를 렌더링합니다.
        /// </summary>
        public abstract void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false, Rect? clipping = null);

        public virtual float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => EditorGUIUtility.singleLineHeight;

        /// <returns>변수가 null 값인지 여부를 반환합니다</returns>
        protected static bool NullToggleField(IInspectorVariableElement variableElement, Rect position, out Rect resultPosition, GUIContent? label, InspectorFlags flags, string? nullText = null)
        {
            resultPosition = position;
            
            if (variableElement.variableType.IsValueType)
                return false;

            return NullToggleField
            (
                position,
                out resultPosition,
                label,
                (!variableElement.inspectable.instancesIsEmpty && variableElement.IsReadable(flags)) ? !variableElement.value.IsNull() : null,
                (!variableElement.inspectable.instancesIsEmpty && variableElement.IsWritable(flags)) ? (x => variableElement.value = x ? variableElement.variableType.GetDefaultValueNotNull() : null) : null,
                variableElement.variableType.IsArray || variableElement.variableType == typeof(string) || variableElement.variableType.HasDefaultConstructor(flags.HasFlagFast(InspectorFlags.NonPublic)),
                variableElement.nullabilityInfo?.writeState,
                nullText ?? $"null ({variableElement.variableType.GetTypeDisplayName()})"
            );
        }
        
        /// <returns>변수가 null 값인지 여부를 반환합니다</returns>
        protected static bool NullToggleField(Rect position, out Rect resultPosition, GUIContent? label, bool? hasValue, Action<bool>? writeAction, bool isInstanceCreatable, RuniNullabilityState? nullabilityState, string nullText)
        {
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (position.width - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            using (new EditorGUI.DisabledScope(writeAction == null))
            {
                if (hasValue != null)
                {
                    if (!hasValue.Value || nullabilityState == RuniNullabilityState.Nullable)
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
                    
                    if (nullabilityState == RuniNullabilityState.Nullable)
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

        protected IMGUIInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element) => this.rootInspector = rootInspector;
        protected IMGUIInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList) => this.rootInspector = rootInspector;
        protected IMGUIInspectorDrawer(IInspectableDictionary inspectableDictionary, Inspector? rootInspector = null) : base(inspectableDictionary) => this.rootInspector = rootInspector;
    }
}