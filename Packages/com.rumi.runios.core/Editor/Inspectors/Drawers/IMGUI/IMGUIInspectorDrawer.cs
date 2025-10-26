#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public abstract class IMGUIInspectorDrawer : InspectorDrawer
    {
        static IMGUIInspectorDrawer()
        {
            ReflectionUtility.onListUpdate += Update;
            Update();
            
            static void Update()
            {
                lock (drawerTypesLock)
                {
                    drawerTypes = ReflectionUtility.types
                        .Where
                        (
                            x =>
                                x.IsDefined(typeof(CustomInspectorDrawerAttribute)) &&
                                x.IsSubclassOf(typeof(IMGUIInspectorDrawer))
                        )
                        .SelectMany
                        (
                            type => type.GetCustomAttributes<CustomInspectorDrawerAttribute>()
                                .Select(attribute => (type, attribute))
                        )
                        .OrderByDescending
                        (
                            x =>
                            {
                                Type targetType = x.attribute.targetType;
                                
                                // 1. 1차 정렬 키: targetType이 인터페이스가 아닌지 여부 (bool)
                                //    - 인터페이스가 아니면 (클래스/구조체): true (높은 값)
                                //    - 인터페이스이면: false (낮은 값)
                                //    -> OrderByDescending이므로 클래스/구조체가 인터페이스보다 앞에 위치
                                bool isNotInterface = !x.attribute.targetType.IsInterface;

                                // 2차 정렬 키: 타입의 깊이 가중치 (int)
                                int depthWeight;
                                if (isNotInterface)
                                {
                                    // [클래스/구조체]: GetHierarchy() (상속 체인 길이) 사용
                                    depthWeight = targetType.GetHierarchy().Count();
                                }
                                else
                                {
                                    // [인터페이스]: 인터페이스가 상속하는 인터페이스의 개수를 사용합니다.
                                    // 상속 개수가 많을수록 구체적입니다. OrderByDescending이므로:
                                    // - IChild: 2 (높음, 우선순위 높음)
                                    // - IBase: 1 
                                    // - IRoot: 0 (낮음, 우선순위 낮음)
                                    depthWeight = targetType.GetInterfaces().Length;
                                }

                                // 최종 정렬 키 튜플
                                // OrderByDescending은 튜플의 요소를 순서대로 비교합니다.
                                return
                                (
                                    // 1. 특정 기본 타입 예외 처리 (높은 우선순위)
                                    targetType != typeof(void),
                                    targetType != typeof(object),
                                    targetType != typeof(Array),
                                    targetType != typeof(ValueType),
                                    targetType != typeof(Enum),
                                    // 2. 클래스 우선
                                    isNotInterface,
                                    // 3. 깊이 가중치 (높을수록 구체적이고 우선순위 높음)
                                    depthWeight
                                );
                            }
                        ).ToImmutableArray();
                }
            }
        }
        
        /// <summary>
        /// Gets a read-only list of all discovered <see cref="IMGUIInspectorDrawer"/> types and their associated <see cref="CustomInspectorDrawerAttribute"/>.
        /// <br/>
        /// The list is ordered by the hierarchy depth of the target type in descending order, ensuring that more specific drawers are prioritized.
        /// <br/><br/>
        /// 발견된 모든 <see cref="IMGUIInspectorDrawer"/> 타입과 관련 <see cref="CustomInspectorDrawerAttribute"/>의 읽기 전용 목록을 가져옵니다.
        /// <br/>
        /// 이 목록은 대상 타입의 계층 깊이(내림차순)에 따라 정렬되어, 더 구체적인 서랍이 우선적으로 처리되도록 합니다.
        /// <br/><br/>
        /// 이 속성은 <b>스레드에 안전</b>합니다. 내부적으로 잠금(<see langword="lock"/>)을 사용하여 <see cref="ReflectionUtility.onListUpdate"/> 이벤트 발생 시 데이터를 갱신합니다.
        /// </summary>
        public static ImmutableArray<(Type type, CustomInspectorDrawerAttribute attribute)> drawerTypes { get; private set; }
        static readonly object drawerTypesLock = new();
        
        
        
        public static IMGUIInspectorDrawer? FindDrawer(IInspectorVariableElement? element, Inspector? rootInspector = null)
        {
            if (element == null)
                return null;
            
            foreach ((Type type, CustomInspectorDrawerAttribute attribute) in drawerTypes)
            {
                if (element.variableType == attribute.targetType || element.variableType.IsAssignableToAny(attribute.targetType))
                    return (IMGUIInspectorDrawer)Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance, null, new object?[] { element, rootInspector }, null);
            }

            return null;
        }
        
        public static Type? FindDrawerType(Type targetType)
        {
            foreach ((Type type, CustomInspectorDrawerAttribute attribute) in drawerTypes)
            {
                if (targetType == attribute.targetType || targetType.IsAssignableToAny(attribute.targetType))
                    return type;
            }

            return null;
        }
        
        /// <summary>
        /// 루트 인스펙터를 가져옵니다.
        /// </summary>
        public Inspector? rootInspector { get; }
        
        /// <summary>
        /// UI 요소를 렌더링합니다.
        /// </summary>
        public abstract void OnGUI(Rect position, GUIContent? label = null, InspectorFlags flags = InspectorFlags.PublicAccess | InspectorFlags.Member | InspectorFlags.List, bool isInArray = false);

        public virtual float GetHeight(GUIContent? label, InspectorFlags flags, bool isInArray = false) => EditorGUIUtility.singleLineHeight;

        protected static bool NullToggleField(IInspectorVariableElement variableElement, Rect position, out Rect resultPosition, GUIContent? label, InspectorFlags flags)
        {
            resultPosition = position;
            if (variableElement.variableType.IsValueType)
                return false;
            
            float toggleWidth = GetXSize(EditorStyles.toggle);
            Rect toggleRect = new Rect(position.x + (position.width - toggleWidth), position.y, toggleWidth, EditorGUIUtility.singleLineHeight);

            using (new EditorGUI.DisabledScope(!variableElement.IsWritable(flags)))
            {
                if (!variableElement.inspectable.instancesIsEmpty && variableElement.IsReadable(flags))
                {
                    bool valueIsNull = variableElement.value.IsNull();
                    if (valueIsNull || variableElement.nullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                    {
                        EditorGUI.BeginChangeCheck();
                        BeginIndentLevel(0);
                        bool toggleValue = EditorGUI.Toggle(toggleRect, !valueIsNull);
                        EndIndentLevel();
                        if (EditorGUI.EndChangeCheck())
                            variableElement.value = toggleValue ? variableElement.variableType.GetDefaultValueNotNull() : null;
                    }

                    position.width -= toggleRect.width + 4;
                    resultPosition = position;

                    if (valueIsNull)
                    {
                        position.height = EditorGUIUtility.singleLineHeight;
                        EditorGUI.LabelField(position, label ?? GUIContent.none, new GUIContent($"null ({variableElement.variableType.GetTypeDisplayName()})"));
                        
                        return true;
                    }
                }
                else
                {
                    BeginIndentLevel(0);
                    
                    if (variableElement.nullabilityInfo?.writeState == RuniNullabilityState.Nullable)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.Toggle(toggleRect, false);
                        if (EditorGUI.EndChangeCheck())
                            variableElement.value = null;
                    }
                    
                    toggleRect.x -= toggleRect.width + 2;
                    position.width -= toggleRect.width + 2;

                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.Toggle(toggleRect, true);
                        if (EditorGUI.EndChangeCheck())
                            variableElement.value = variableElement.variableType.GetDefaultValueNotNull();

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
    }
}