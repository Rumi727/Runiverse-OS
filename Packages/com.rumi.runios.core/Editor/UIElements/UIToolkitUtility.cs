using RuniOS.Editor.APIBridge.UnityEditor;
using RuniOS.Editor.APIBridge.UnityEngine.UIElements;
using RuniOS.Collections.Generic;
using RuniOS.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements
{
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
        
        // Patches/Runtime/UnityEngine.UIElements.UIElementsUtility.cs를 참고해주세요
        /// <summary>
        /// 현재 호출 스택에서 UI Toolkit의 <see cref="IMGUIContainer"/>를 통해 IMGUI 렌더링을 시작한 <see cref="IMGUIContainer"/>를 가져옵니다.<br/>
        /// 현재 스택에 해당 <see cref="IMGUIContainer"/>를 통한 IMGUI 호출이 없거나, IMGUI를 렌더링하게 한 <see cref="IMGUIContainer"/>가 없을 경우 <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 이 프로퍼티는 UI Toolkit의 <see cref="IMGUIContainer"/>를 통해 IMGUI 렌더링이 발생하는 특정 상황을 추적하고, 해당 컨테이너 인스턴스를 참조하기 위해 사용됩니다.<br/>
        /// 이는 IMGUI 기반의 레거시 코드가 UI Toolkit 환경과 상호작용할 때, 해당 IMGUI를 감싸고 있는 컨테이너에 접근하여 추가적인 로직을 적용하거나 상태를 관리하는 데 활용될 수 있습니다.
        /// <br/><br/>
        /// 이 프로퍼티는 Unity UI Toolkit의 `UIElementsUtility` 클래스에 대한 패치(Patches.UnityEngine.UIElements.UIElementsUtility.cs)를 통해 추가되었습니다.
        /// </remarks>
        public static IMGUIContainer? currentIMGUIContainer { get; internal set; }
        
        /// <summary>
        /// 현재 호출 스택에서 <b>마지막으로</b> <see cref="PropertyDrawer.CreatePropertyGUI"/> 메서드를 호출한 <see cref="PropertyField"/>를 가져옵니다.<br/>
        /// 현재 스택에 해당 메서드 호출이 없거나, 메서드를 호출한 <see cref="PropertyField"/>가 없을 경우 <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 이 프로퍼티는 Unity 에디터의 `PropertyField` 클래스에 대한 패치(Patches.UnityEditor.UIElements.PropertyField.cs)를 통해 추가되었습니다.<br/>
        /// <br/>
        /// **주요 용도:**<br/>
        /// - <see cref="PropertyDrawer"/>에서 <see cref="PropertyField"/>의 커스텀 라벨을 인식하고 UI를 설정하는 등, 기본적인 Unity API로는 구현하기 어려운 기능을 지원합니다.<br/>
        /// - 현재 처리 중인 <see cref="PropertyField"/> 인스턴스에 대한 참조를 제공하여, Drawer 구현 시 추가적인 컨텍스트 정보를 활용할 수 있도록 합니다.<br/>
        /// </remarks>
        public static PropertyField? currentPropertyField
        {
            get
            {
                if (currentPropertyFieldStack.TryPeek(out PropertyField result))
                    return result;
                
                return null;
            }
        }
        
        // Patches/Editor/UnityEditor.UIElements.PropertyField.cs를 참고해주세요
        internal static Stack<PropertyField> _currentPropertyField { get; } = new Stack<PropertyField>();
        
        /// <summary>
        /// 현재 호출 스택에서 <see cref="PropertyDrawer.CreatePropertyGUI"/> 메서드를 호출한 <b>모든</b> <see cref="PropertyField"/>를 가져옵니다.<br/>
        /// </summary>
        /// <remarks>
        /// 이 프로퍼티는 Unity 에디터의 `PropertyField` 클래스에 대한 패치(Patches.UnityEditor.UIElements.PropertyField.cs)를 통해 추가되었습니다.<br/>
        /// <br/>
        /// **주요 용도:**<br/>
        /// - <see cref="PropertyDrawer"/>에서 <see cref="PropertyField"/>의 커스텀 라벨을 인식하고 UI를 설정하는 등, 기본적인 Unity API로는 구현하기 어려운 기능을 지원합니다.<br/>
        /// - 현재 처리 중인 <see cref="PropertyField"/> 인스턴스에 대한 참조를 제공하여, Drawer 구현 시 추가적인 컨텍스트 정보를 활용할 수 있도록 합니다.<br/>
        /// </remarks>
        public static ReadOnlyStack<PropertyField> currentPropertyFieldStack { get; } = _currentPropertyField.AsReadOnly();
        
        public static void UpdateContainerHeight(float height)
        {
            if (currentIMGUIContainer != null)
            {
                StyleLength lastHeight = currentIMGUIContainer.style.height;
                currentIMGUIContainer.style.height = new Length(height);
                currentIMGUIContainer.style.height = lastHeight;
            }
        }
        
        // Patches/Editor/UnityEngine.UIElements.TextElement.cs를 참고해주세요
        internal static readonly ConditionalWeakTable<TextElement, Action<string>> labelChangedCallbacks = new();
        
        /// <summary>
        /// 필드의 라벨 변경 된 후에 즉시 호출되는 이벤트를 등록합니다.<br/>
        /// 무한 루프에 주의하세요!
        /// </summary>
        /// <remarks>
        /// 모딩으로 프로퍼티에 코드를 주입한 것이기 때문에 훨씬 정확합니다. (기본 이벤트는 패널에 부착되어야 실행 됨)
        /// </remarks>
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

        /// <summary>
        /// <see cref="SerializedProperty"/>의 표시 이름(Display Name)을 가져옵니다.
        /// </summary>
        /// <param name="property">표시 이름을 가져올 대상 <see cref="SerializedProperty"/>입니다.</param>
        /// <returns>
        /// 현재 호출 스택의 <see cref="currentPropertyFieldStack"/>에 커스텀 라벨이 설정되어 있으면 해당 라벨을 반환하고,<br/>
        /// 그렇지 않으면 <see cref="SerializedProperty.displayName"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// 이 메서드는 <see cref="PropertyDrawer"/>에서 <see cref="currentPropertyFieldStack"/>를 통해 설정된 커스텀 라벨을 가져오기 위해 사용됩니다.<br/>
        /// 이는 유니티의 기본 API로는 불가능했던, 커스텀 라벨을 가진 <see cref="PropertyField"/>의 표시 이름을 정확하게 가져오는 데 사용됩니다.
        /// </remarks>
        public static string GetFieldLabel(this SerializedProperty property)
        {
            if (currentPropertyField != null && !string.IsNullOrEmpty(currentPropertyField.label))
                return currentPropertyField.label;
            
            string? label = null;
            foreach (PropertyField propertyField in currentPropertyFieldStack)
            {
                if (!string.IsNullOrEmpty(propertyField.label))
                    label = propertyField.label;
                
                if (propertyField.GetFoldout() != null)
                    label = null;
            }

            return label ?? property.displayName;
        }

        // Patches/Editor/UnityEngine.UIElements.VisualElement.cs를 참고해주세요
        internal static ConditionalWeakTable<PropertyField, PropertyFieldExtensionData> propertyExtensionDatas { get; } = new();
        
        /// <summary>
        /// 현재 호출 시점에 <see cref="PropertyField"/> 요소가 가지고 있는 <see cref="Foldout"/> 요소를 가져옵니다.
        /// </summary>
        /// <remarks>
        /// 이 프로퍼티는 Unity 에디터의 `PropertyField` 클래스에 대한 패치(Patches.UnityEditor.UIElements.PropertyField.cs)를 통해 추가되었습니다.<br/>
        /// </remarks>
        public static Foldout? GetFoldout(this PropertyField propertyField)
        {
            if (propertyExtensionDatas.TryGetValue(propertyField, out var data))
                return data.foldout;
            
            return null;
        }

        /// <summary>
        /// Sets the binding path of a <see cref="BindableElement"/> to the path of a <see cref="SerializedProperty"/>.
        /// <br/>
        /// <see cref="BindableElement"/>의 바인딩 경로를 <see cref="SerializedProperty"/>의 경로로 설정합니다.
        /// </summary>
        /// <param name="element">
        /// The bindable element to set the path for.
        /// <br/>
        /// 경로를 설정할 바인딩 가능한 요소입니다.
        /// </param>
        /// <param name="property">
        /// The serialized property whose path will be used.
        /// <br/>
        /// 경로가 사용될 직렬화된 속성입니다.</param>
        /// <param name="childAlign">
        /// 자식의 라벨까지 정렬할 지 결정합니다.
        /// </param>
        /// <returns>
        /// The modified bindable element.<br/>
        /// 수정된 바인딩 가능한 요소입니다.
        /// </returns>
        public static VisualElement SetProperty(this VisualElement element, SerializedProperty property, bool childAlign = true)
        {
            if (IPrefixLabelBridge.__targetType.IsInstanceOfType(element))
                IPrefixLabelBridge.__GetInstanceFrom(element).SetLabel(property.GetFieldLabel());

            if (element is IBindable bindable)
                bindable.bindingPath = property.propertyPath;

            return element.ConfigureFieldStyles(childAlign);
        }



        /// <summary>
        /// Configures the USS (Unity Style Sheets) styles for a <see cref="BaseField{TValueType}"/> to apply alignment.
        /// <br/>
        /// <see cref="BaseField{TValueType}"/>에 정렬을 적용하기 위한 USS(Unity Style Sheets) 스타일을 구성합니다.
        /// </summary>
        /// <param name="field">The field to configure.
        /// <br/>
        /// 구성할 필드입니다.</param>
        /// <param name="childAlign">
        /// 자식의 라벨까지 정렬할 지 결정합니다.
        /// </param>
        /// <returns>The configured field.
        /// <br/>
        /// 구성된 필드입니다.</returns>
        public static VisualElement ConfigureFieldStyles(this VisualElement field, bool childAlign = true)
        {
            if (IPrefixLabelBridge.__targetType.IsInstanceOfType(field))
                IPrefixLabelBridge.__GetInstanceFrom(field).labelElement.AddToClassList(PropertyField.labelUssClassName);

            field.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            
            if (childAlign)
            {
                VisualElement? visualInput = field.Q<VisualElement>(classes: BaseField<int>.inputUssClassName);
                visualInput?.AddToClassList(PropertyField.inputUssClassName);
                visualInput?.Query(null, BaseField<int>.ussClassName)
                    .ForEach(x => x.AddToClassList(BaseField<int>.alignedFieldUssClassName));
            }

            return field;
        }

        public static Type GetPropertyTypeWithoutList(this SerializedProperty property)
        {
            ScriptAttributeUtilityBridge.GetFieldInfoFromProperty(property, out Type type);
            if (property.isArray)
            {
                while (true)
                {
                    if (type.IsArray)
                    {
                        type = type.GetElementType()!;
                        continue;
                    }
                    else
                    {
                        Type? elementType = CollectionGenericUtility.GetListElementType(type);
                        if (elementType != null)
                        {
                            type = elementType;
                            continue;
                        }
                    }

                    break;
                }
            }

            return type;
        }
        
        
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
        
        internal class PropertyFieldExtensionData
        {
            public Foldout? foldout;
            //public bool align = true;
        }
    }
}