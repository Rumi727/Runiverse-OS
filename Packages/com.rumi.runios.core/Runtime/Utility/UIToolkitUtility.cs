#nullable enable
using RuniOS.APIBridge.UnityEngine.UIElements;
using RuniOS.UIElements;
using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS
{
    /// <summary>
    /// UI Toolkit과 관련된 유틸리티 함수들을 제공하는 정적 클래스입니다.
    /// </summary>
    public static class UIToolkitUtility
    {
        /// <summary>
        /// RuniOS 컨트롤 스타일
        /// </summary>
        public static StyleSheet rosControlStyle
        {
            get
            {
                if (_rosControlStyle == null)
                    _rosControlStyle = Resources.Load<StyleSheet>("RuniOS/UI Elements/ROS Control Style");

                return _rosControlStyle;
            }
        }
        static StyleSheet? _rosControlStyle;
        
#if UNITY_EDITOR
        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 런타임 패널에 속하는지 여부를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 에디터에서는 <see cref="IRuntimePanel"/>에 속하고 <see cref="Kernel.isPlaying"/>이 true일 때만 런타임 패널로 간주합니다.
        /// 빌드된 애플리케이션에서는 항상 true를 반환합니다.
        /// </remarks>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>지정된 <see cref="VisualElement"/>가 런타임 패널에 속하면 true, 그렇지 않으면 false를 반환합니다.</returns>
        public static bool IsRuntimePanel(this VisualElement visualElement) => visualElement.panel is IRuntimePanel && Kernel.isPlaying;

        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 에디터 패널에 속하는지 여부를 반환합니다.
        /// </summary>
        /// <remarks>
        /// 에디터에서는 <see cref="IRuntimePanel"/>에 속하지 않거나 <see cref="Kernel.isPlaying"/>이 false일 때만 에디터 패널로 간주합니다.
        /// 빌드된 애플리케이션에서는 항상 false를 반환합니다.
        /// </remarks>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>지정된 <see cref="VisualElement"/>가 에디터 패널에 속하면 true, 그렇지 않으면 false를 반환합니다.</returns>
        public static bool IsEditorPanel(this VisualElement visualElement) => visualElement.panel is not IRuntimePanel || !Kernel.isPlaying;
#else
#pragma warning disable IDE0060 // 사용하지 않는 매개 변수를 제거하세요.
        // ReSharper disable UnusedParameter.Global
        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 런타임 패널에 속하는지 여부를 반환합니다.
        /// 빌드된 애플리케이션에서는 항상 true를 반환합니다.
        /// </summary>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>항상 true를 반환합니다.</returns>
        public static bool IsRuntimePanel(this VisualElement visualElement) => true;

        /// <summary>
        /// 지정된 <see cref="VisualElement"/>가 에디터 패널에 속하는지 여부를 반환합니다.
        /// 빌드된 애플리케이션에서는 항상 false를 반환합니다.
        /// </summary>
        /// <param name="visualElement">확인할 <see cref="VisualElement"/>입니다.</param>
        /// <returns>항상 false를 반환합니다.</returns>
        public static bool IsEditorPanel(this VisualElement visualElement) => false;
        // ReSharper restore UnusedParameter.Global
#pragma warning restore IDE0060 // 사용하지 않는 매개 변수를 제거하세요.
#endif

        static PropertyInfo? pseudoStatesProperty;

        public static PseudoStates GetPsuedoState(this VisualElement element) => (PseudoStates)VisualElementBridge.__GetInstanceFrom(element).pseudoStates;
        
        public static void SetPsuedoState(this VisualElement element, PseudoStates state) => VisualElementBridge.__GetInstanceFrom(element).pseudoStates = (PseudoStatesBridge)state;

        public static void AddPsuedoState(this VisualElement element, PseudoStates state) => element.SetPsuedoState(element.GetPsuedoState() | state);

        public static void RemovePsuedoState(this VisualElement element, PseudoStates state) => element.SetPsuedoState(element.GetPsuedoState() & ~state);

        public static bool HasPseudoFlag(this VisualElement element, PseudoStates flag) => (element.GetPsuedoState() & flag) == flag;

#if !UNITY_6000_3_OR_NEWER
        public static void SetCheckedPseudoState(this VisualElement element, bool value)
        {
            if (value)
                element.AddPsuedoState(PseudoStates.Checked);
            else
                element.RemovePsuedoState(PseudoStates.Checked);
        }
#endif
        
        public static void SetValueWithoutNotify<T>(this INotifyValueChanged<T> element, T newValue) => element.SetValueWithoutNotify(newValue);

        /// <summary>
        /// 요소가 패널에 등록될 때마다 루트 요소에 스타일 시트를 맨 위에 등록합니다
        /// </summary>
        public static void RegisterDefaultStyleSheet(this VisualElement element, StyleSheet styleSheet)
        {
            element.RegisterCallback<AttachToPanelEvent>(x =>
            {
                VisualElement root = x.destinationPanel.visualTree;
                if (!element.IsEditorPanel())
                {
                    if (!root.styleSheets.Contains(styleSheet))
                        root.styleSheets.Insert(0, styleSheet);
                    
                    return;
                }
         
                /*
                 * 디버거 찍어보심 아시겠지만 root 안의 rootVisualContainer2 요소에 스타일 시트가 적용되서
                 * 이렇게 해주지 않으면 유니티의 내장 스타일 시트가 먹어버립니다
                 */
                
                for (int i = 0; i < root.hierarchy.childCount; i++)
                {
                    VisualElement child = root.hierarchy[i];
                    if (!child.styleSheets.Contains(styleSheet))
                        child.styleSheets.Add(styleSheet);
                }
            });
        }

        static readonly MethodInfo? registerValueChangedCallback = AccessUtility.DeclaredMethod(typeof(INotifyValueChangedExtensions), nameof(INotifyValueChangedExtensions.RegisterValueChangedCallback));
        public static bool RegisterValueChangedCallback(this VisualElement element, Type targetType, Action<object> callback)
        {
            if (typeof(INotifyValueChanged<>).MakeGenericType(targetType).IsInstanceOfType(element))
            {
                if (registerValueChangedCallback != null)
                {
                    MethodInfo callbackMethodInfo = callback.Method;

                    var changeEventType = typeof(ChangeEvent<>).MakeGenericType(targetType);
                    var eventParameter = Expression.Parameter(changeEventType, "evt");

                    // `evt.newValue`를 가져오는 Expression을 생성합니다.
                    var newValueProperty = Expression.Property(eventParameter, "newValue");

                    // `evt.newValue`를 `object`로 변환하는 Expression을 생성합니다.
                    var convertedValue = Expression.Convert(newValueProperty, typeof(object));

                    // 딜리게이트의 타겟을 Expression으로 만듭니다.
                    var instanceExpression = Expression.Constant(callback.Target);

                    // `Write` 메소드를 호출하는 Expression을 생성합니다.
                    var methodCall = Expression.Call(instanceExpression, callbackMethodInfo, convertedValue);

                    // 최종 람다 Expression을 생성합니다.
                    var delegateType = typeof(EventCallback<>).MakeGenericType(changeEventType);
                    var lambda = Expression.Lambda(delegateType, methodCall, eventParameter);

                    // Expression을 컴파일하여 델리게이트를 얻습니다.
                    Delegate compiledDelegate = lambda.Compile();

                    registerValueChangedCallback.MakeGenericMethod(targetType).Invoke(null, new object[] { element, compiledDelegate });
                    
                    return true;
                }
                else
                    Debug.LogWarning($"Method not found: '{nameof(INotifyValueChangedExtensions.RegisterValueChangedCallback)}'.");
            }

            return false;
        }

        // Patches/Editor/UnityEngine.UIElements.TextElement.cs를 참고해주세요
#if !UNITY_EDITOR && ENABLE_IL2CPP
        [Obsolete("IL2CPP environment is not supported.", true)]
#endif
        internal static readonly ConditionalWeakTable<TextElement, Action<string>> labelChangedCallbacks = new();
        
        /// <summary>
        /// 필드의 라벨 변경 된 후에 즉시 호출되는 이벤트를 등록합니다.<br/>
        /// 무한 루프에 주의하세요!
        /// </summary>
        /// <remarks>
        /// 모딩으로 프로퍼티에 코드를 주입한 것이기 때문에 훨씬 정확합니다. (기본 이벤트는 패널에 부착되어야 실행 됨)
        /// </remarks>
#if !UNITY_EDITOR && ENABLE_IL2CPP
        [Obsolete("IL2CPP environment is not supported.", true)]
#endif
        public static void RegisterLabelChangedCallback<TValueType>(this BaseField<TValueType> field, Action<string> callback)
        {
            if (labelChangedCallbacks.TryGetValue(field.labelElement, out Action<string> result))
            {
                result += callback;
                labelChangedCallbacks.AddOrUpdate(field.labelElement, result);
            }
            else
                labelChangedCallbacks.Add(field.labelElement, callback);
        }
    }
}
