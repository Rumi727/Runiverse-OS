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

            static void Update()
            {
                lock (drawerTypesLock)
                {
                    drawerTypes = ReflectionUtility.types
                        .Where
                        (
                            static x =>
                                x.IsDefined(typeof(CustomInspectorDrawerAttribute)) &&
                                x.IsSubclassOf(typeof(IMGUIInspectorDrawer))
                        )
                        .Select(static x => (x, x.GetCustomAttribute<CustomInspectorDrawerAttribute>()))
                        .OrderByDescending
                        (
                            x =>
                            {
                                if (x.Item2.priority == 0)
                                {
                                    if (x.Item2.targetType.IsInterface)
                                        return 0;

                                    return x.Item2.targetType.GetHierarchy().Count() * 100;
                                }
                                else
                                    return x.Item2.priority;
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

        protected IMGUIInspectorDrawer(IInspectorElement element, Inspector? rootInspector = null) : base(element) => this.rootInspector = rootInspector;
        protected IMGUIInspectorDrawer(IInspectableList inspectableList, Inspector? rootInspector = null) : base(inspectableList) => this.rootInspector = rootInspector;
    }
}