#nullable enable
using RuniOS.Inspectors;
using RuniOS.Inspectors.Drawers;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RuniOS.Editor.Inspectors.Drawers.IMGUI
{
    public abstract class IMGUIInspectorDrawer : InspectorDrawer
    {
        static IMGUIInspectorDrawer()
        {
            ReflectionUtility.onListUpdate += Update;
            Update();

            foreach (var item in drawerTypes)
            {
                Debug.Log((item.attribute.targetType, item.type));
            }

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
                                // 1. 1차 정렬 키: targetType이 인터페이스가 아닌지 여부 (bool)
                                //    - 인터페이스가 아니면 (클래스/구조체): true (높은 값)
                                //    - 인터페이스이면: false (낮은 값)
                                //    -> OrderByDescending이므로 클래스/구조체가 인터페이스보다 앞에 위치
                                bool isNotInterface = !x.attribute.targetType.IsInterface;

                                // 2. 2차 정렬 키: 기존의 우선순위 로직 (int)
                                int secondarySort = x.attribute.targetType.GetHierarchy().Count();

                                // 튜플로 반환하여 1차 (bool), 2차 (int) 정렬 기준 적용
                                return
                                (
                                    x.attribute.targetType != typeof(void),
                                    x.attribute.targetType != typeof(object),
                                    x.attribute.targetType != typeof(Array),
                                    x.attribute.targetType != typeof(ValueType),
                                    x.attribute.targetType != typeof(Enum),
                                    isNotInterface,
                                    secondarySort
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
                    return (IMGUIInspectorDrawer)Activator.CreateInstance(type, element, rootInspector);
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

        protected IMGUIInspectorDrawer(IInspectorVariableElement element, Inspector? rootInspector = null) : base(element) => this.rootInspector = rootInspector;
        protected IMGUIInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList) => this.rootInspector = rootInspector;
    }
}