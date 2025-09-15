#nullable enable
using RuniOS.APIBridge.UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements
{
    /// <summary>
    /// Provides utility methods for PropertyField related operations.
    /// <br/>
    /// PropertyField 관련 작업을 위한 유틸리티 메서드를 제공합니다.
    /// </summary>
    public static class PropertyFieldUtility
    {
        // Patches.UnityEditor.UIElements.PropertyField.cs를 참고해주세요
        /// <summary>
        /// 현재 호출 스택에서 <see cref="PropertyDrawer.CreatePropertyGUI"/> 메서드를 호출한 <see cref="PropertyField"/>를 가져옵니다.<br/>
        /// 현재 스택에 해당 메서드 호출이 없거나, 메서드를 호출한 <see cref="PropertyField"/>가 없을 경우 <see langword="null"/>을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 이 프로퍼티는 Unity 에디터의 `PropertyField` 클래스에 대한 패치(Patches.UnityEditor.UIElements.PropertyField.cs)를 통해 추가되었습니다.<br/>
        /// <br/>
        /// **주요 용도:**<br/>
        /// - <see cref="PropertyDrawer"/>에서 <see cref="PropertyField"/>의 커스텀 라벨을 인식하고 UI를 설정하는 등, 기본적인 Unity API로는 구현하기 어려운 기능을 지원합니다.<br/>
        /// - 현재 처리 중인 <see cref="PropertyField"/> 인스턴스에 대한 참조를 제공하여, Drawer 구현 시 추가적인 컨텍스트 정보를 활용할 수 있도록 합니다.<br/>
        /// </remarks>
        public static Stack<PropertyField> currentPropertyField { get; } = new();

        /// <summary>
        /// <see cref="SerializedProperty"/>의 표시 이름(Display Name)을 가져옵니다.
        /// </summary>
        /// <param name="property">표시 이름을 가져올 대상 <see cref="SerializedProperty"/>입니다.</param>
        /// <returns>
        /// 현재 호출 스택의 <see cref="currentPropertyField"/>에 커스텀 라벨이 설정되어 있으면 해당 라벨을 반환하고,<br/>
        /// 그렇지 않으면 <see cref="SerializedProperty.displayName"/>을 반환합니다.
        /// </returns>
        /// <remarks>
        /// 이 메서드는 <see cref="PropertyDrawer"/>에서 <see cref="currentPropertyField"/>를 통해 설정된 커스텀 라벨을 가져오기 위해 사용됩니다.<br/>
        /// 이는 유니티의 기본 API로는 불가능했던, 커스텀 라벨을 가진 <see cref="PropertyField"/>의 표시 이름을 정확하게 가져오는 데 사용됩니다.
        /// </remarks>
        public static string GetFieldLabel(this SerializedProperty property)
        {
            string label = property.displayName;
            foreach (PropertyField propertyField in currentPropertyField.Where(propertyField => !string.IsNullOrEmpty(propertyField.label)))
                label = propertyField.label;

            return label;
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
        /// <returns>
        /// The modified bindable element.<br/>
        /// 수정된 바인딩 가능한 요소입니다.
        /// </returns>
        public static VisualElement SetProperty(this VisualElement element, SerializedProperty property)
        {
            if (IPrefixLabelBridge.__targetType.IsInstanceOfType(element))
                IPrefixLabelBridge.__GetInstanceFrom(element).SetLabel(property.GetFieldLabel());

            if (element is IBindable bindable)
                bindable.bindingPath = property.propertyPath;

            return element.ConfigureFieldStyles();
        }



        /// <summary>
        /// Configures the USS (Unity Style Sheets) styles for a <see cref="BaseField{TValueType}"/> to apply alignment.
        /// <br/>
        /// <see cref="BaseField{TValueType}"/>에 정렬을 적용하기 위한 USS(Unity Style Sheets) 스타일을 구성합니다.
        /// </summary>
        /// <param name="field">The field to configure.
        /// <br/>
        /// 구성할 필드입니다.</param>
        /// <returns>The configured field.
        /// <br/>
        /// 구성된 필드입니다.</returns>
        public static VisualElement ConfigureFieldStyles(this VisualElement field)
        {
            if (IPrefixLabelBridge.__targetType.IsInstanceOfType(field))
                IPrefixLabelBridge.__GetInstanceFrom(field).labelElement.AddToClassList(PropertyField.labelUssClassName);

            field.AddToClassList(BaseField<int>.alignedFieldUssClassName);
            
            VisualElement? visualInput = field.Q<VisualElement>(classes: BaseField<int>.inputUssClassName);
            visualInput?.AddToClassList(PropertyField.inputUssClassName);
            visualInput?.Query(null, BaseField<int>.ussClassName)
                .ForEach(x => x.AddToClassList(BaseField<int>.alignedFieldUssClassName));

            return field;
        }
    }
}