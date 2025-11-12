using RuniOS.APIBridge.UnityEditor;
using RuniOS.APIBridge.UnityEngine.UIElements;
using RuniOS.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements
{
    public static class EditorUIToolkitUtility
    {
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
        
        // Patches/Editor/UnityEngine.UIElements.TextElement.cs를 참고해주세요
        internal static readonly ConditionalWeakTable<TextElement, Action<string>> labelChangedCallbacks = new();
        
        // Patches/Editor/UnityEditor.UIElements.PropertyField.cs를 참고해주세요
        internal static Stack<PropertyField> _currentPropertyField { get; } = new Stack<PropertyField>();
        
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
        public static ReadOnlyStack<PropertyField> currentPropertyFieldStack { get; } = new ReadOnlyStack<PropertyField>(_currentPropertyField);

        // Patches/Editor/UnityEngine.UIElements.VisualElement.cs를 참고해주세요
        internal static ConditionalWeakTable<PropertyField, PropertyFieldExtensionData> propertyExtensionDatas { get; } = new();
        
        public static void UpdateContainerHeight(float height)
        {
            if (currentIMGUIContainer != null)
            {
                StyleLength lastHeight = currentIMGUIContainer.style.height;
                currentIMGUIContainer.style.height = new Length(height);
                currentIMGUIContainer.style.height = lastHeight;
            }
        }
        
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
        
        internal class PropertyFieldExtensionData
        {
            public Foldout? foldout;
            //public bool align = true;
        }
    }
}