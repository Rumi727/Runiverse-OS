#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    /// <summary>
    /// 여러 개의 자식 <see cref="VisualElement"/>를 조합하여 하나의 복합 필드처럼 동작하는 추상 클래스입니다.
    /// </summary>
    /// <typeparam name="TValueType">이 필드가 나타내는 값의 타입입니다.</typeparam>
    public abstract class RuniBaseCompositeField<TValueType> : BaseField<TValueType>
    {
        /// <summary>
        /// 이 복합 필드에 대한 USS 클래스 이름입니다.
        /// </summary>
        public new const string ussClassName = "unity-composite-field";
        /// <summary>
        /// 라벨 요소에 대한 USS 클래스 이름입니다.
        /// </summary>
        public new const string labelUssClassName = ussClassName + "__label";
        /// <summary>
        /// 시각적 입력 요소에 대한 USS 클래스 이름입니다.
        /// </summary>
        public new const string inputUssClassName = ussClassName + "__input";
        /// <summary>
        /// 필드 사이의 공백 요소에 대한 USS 클래스 이름입니다.
        /// </summary>
        public const string spacerUssClassName = ussClassName + "__field-spacer";
        /// <summary>
        /// 여러 줄 레이아웃에 대한 USS 클래스 변형입니다.
        /// </summary>
        public const string multilineVariantUssClassName = ussClassName + "--multi-line";
        /// <summary>
        /// 여러 필드들을 묶는 그룹에 대한 USS 클래스 이름입니다.
        /// </summary>
        public const string fieldGroupUssClassName = ussClassName + "__field-group";
        /// <summary>
        /// 개별 필드에 대한 USS 클래스 이름입니다.
        /// </summary>
        public const string fieldUssClassName = ussClassName + "__field";
        /// <summary>
        /// 첫 번째 필드에 대한 USS 클래스 이름입니다.
        /// </summary>
        public const string firstFieldVariantUssClassName = fieldUssClassName + "--first";
        /// <summary>
        /// 마지막 필드에 대한 USS 클래스 이름입니다.
        /// </summary>
        public const string lastFieldVariantUssClassName = fieldUssClassName + "--last";
        /// <summary>
        /// 두 줄 레이아웃에 대한 USS 클래스 이름입니다.
        /// </summary>
        public const string twoLinesVariantUssClassName = ussClassName + "--two-lines";
        
        /// <summary>
        /// 모든 자식 필드를 포함하는 시각적 입력 요소입니다.
        /// </summary>
        public VisualElement visualInput { get; }

        /// <summary>
        /// 이 복합 필드를 구성하는 자식 요소들의 설명을 담고 있는 읽기 전용 리스트입니다.
        /// <br/>최초 접근 시 <see cref="GetElementDescriptions"/> 메소드를 호출하여 초기화됩니다.
        /// </summary>
        public IReadOnlyList<IElementDescription> descriptions => _descriptions ??= GetElementDescriptions().ToArray().AsReadOnly();
        IReadOnlyList<IElementDescription>? _descriptions;
        
        /// <summary>
        /// 라벨 텍스트를 사용하여 <see cref="RuniBaseCompositeField{TValueType}"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="label">필드에 표시될 라벨입니다.</param>
        protected RuniBaseCompositeField(string? label) : base(label, new VisualElement())
        {
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            AddToClassList(ussClassName);
            delegatesFocus = false;
            
            labelElement.AddToClassList(labelUssClassName);
            
            visualInput = this.Q<VisualElement>(className: BaseField<TValueType>.inputUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.focusable = false;
        }
        
        /// <summary>
        /// 이 메소드를 오버라이드하여 복합 필드를 구성할 자식 요소들의 목록을 정의합니다.
        /// <br/>이 메소드는 생성자에서 직접 호출되지 않으며, <see cref="descriptions"/> 속성에 처음 접근할 때 호출됩니다.
        /// </summary>
        /// <returns>자식 요소들의 설명을 담은 <see cref="IEnumerable{T}"/>입니다.</returns>
        protected abstract IEnumerable<IElementDescription> GetElementDescriptions();
        
        /// <summary>
        /// 이 복합 필드의 자식 필드들을 한 줄에 배치하여 시각적 레이아웃을 구성합니다.
        /// </summary>
        public void SetFieldsByHorizontal() => SetFieldsByLine(0);
        
        /// <summary>
        /// 이 복합 필드의 자식 필드들을 한 줄에 <paramref name="fieldsByLine"/> 개수만큼 배치하여 시각적 레이아웃을 구성합니다.
        /// </summary>
        /// <param name="fieldsByLine">한 줄에 배치할 자식 필드의 개수입니다. 0보다 클 경우 여러 줄로 표시됩니다.</param>
        public void SetFieldsByLine(int fieldsByLine)
        {
            visualInput.Clear();
            
            int line = 1;
            if (fieldsByLine >= 1)
                line = ((float)descriptions.Count / fieldsByLine).CeilToInt();
            else
                fieldsByLine = descriptions.Count;
            
            bool multiLine = line > 1;
            if (multiLine)
                AddToClassList(multilineVariantUssClassName);
            
            for (int i = 0; i < line; i++)
            {
                Hierarchy hierarchy = visualInput.hierarchy;
                if (multiLine)
                {
                    VisualElement child = new VisualElement();
                    child.AddToClassList(fieldGroupUssClassName);

                    hierarchy.Add(child);
                    hierarchy = child.hierarchy;
                }

                bool isFirst = true;
                int lastIndex = ((i * fieldsByLine) + fieldsByLine).Min(descriptions.Count);
                for (int j = i * fieldsByLine; j < lastIndex; j++)
                {
                    IElementDescription? description = descriptions[j];
                    SetupElement(description);
                    
                    VisualElement? element = description?.element;
                    if (element == null)
                        continue;
                    
                    if (description is IFieldDescription)
                    {
                        if (isFirst)
                        {
                            element.AddToClassList(firstFieldVariantUssClassName);
                            isFirst = false;
                        }
                        if (j >= lastIndex - 1)
                            element.AddToClassList(lastFieldVariantUssClassName);
                    }
                    
                    hierarchy.Add(element);
                }
            }

            UpdateDisplay();
        }

        /// <summary>
        /// 지정된 <paramref name="description"/>을 기반으로 <see cref="VisualElement"/>를 생성하고,
        /// 필드인 경우 값 변경 이벤트를 등록하여 부모 필드와의 데이터 바인딩을 설정합니다.
        /// <br/>이 메소드는 <see cref="SetFieldsByLine"/> 메소드 또는 수동으로 레이아웃을 구성할 때 사용됩니다.
        /// </summary>
        /// <param name="description">설정할 요소의 설명 데이터입니다.</param>
        public void SetupElement(IElementDescription description)
        {
            try
            {
                if (!typeof(VisualElement).IsAssignableFrom(description.elementType))
                {
                    Debug.LogWarning($"Cannot register type {description.elementType} because it does not inherit type {typeof(VisualElement)}.");
                    return;
                }
                else if (!description.elementType.HasDefaultConstructor())
                {
                    Debug.LogWarning($"Cannot register type {description.elementType} because it has no default public constructor.");
                    return;
                }

                description.element ??= (VisualElement)Activator.CreateInstance(description.elementType);
                description.element.delegatesFocus = true;

                if (description is IFieldDescription fieldDescription && fieldDescription.writeEvent != null && typeof(INotifyValueChanged<>).MakeGenericType(fieldDescription.fieldValueType).IsInstanceOfType(description.element))
                {
                    description.element.AddToClassList(fieldUssClassName);
                    
                    try
                    {
                        MethodInfo? changedCallback = AccessUtility.DeclaredMethod(typeof(INotifyValueChangedExtensions), nameof(INotifyValueChangedExtensions.RegisterValueChangedCallback));
                        if (changedCallback != null)
                        {
                            //나중에 element 속성이 변경되어도 이벤트는 원래 값을 참조하도록 포인터 복제
                            IFieldDescription.WriteDelegate writeDelegate = fieldDescription.writeEvent;
                            Type fieldValueType = fieldDescription.fieldValueType;
                            Action<object> writeFunc = Write;
                            MethodInfo writeMethodInfo = writeFunc.Method;

                            var changeEventType = typeof(ChangeEvent<>).MakeGenericType(fieldValueType);
                            var eventParameter = Expression.Parameter(changeEventType, "evt");

                            // `evt.newValue`를 가져오는 Expression을 생성합니다.
                            var newValueProperty = Expression.Property(eventParameter, "newValue");

                            // `evt.newValue`를 `object`로 변환하는 Expression을 생성합니다.
                            var convertedValue = Expression.Convert(newValueProperty, typeof(object));

                            // 딜리게이트의 타겟을 Expression으로 만듭니다.
                            var instanceExpression = Expression.Constant(writeFunc.Target);

                            // `Write` 메소드를 호출하는 Expression을 생성합니다.
                            var methodCall = Expression.Call(instanceExpression, writeMethodInfo, convertedValue);

                            // 최종 람다 Expression을 생성합니다.
                            var delegateType = typeof(EventCallback<>).MakeGenericType(changeEventType);
                            var lambda = Expression.Lambda(delegateType, methodCall, eventParameter);

                            // Expression을 컴파일하여 델리게이트를 얻습니다.
                            Delegate compiledDelegate = lambda.Compile();

                            changedCallback = changedCallback.MakeGenericMethod(fieldValueType);
                            changedCallback.Invoke(null, new object[] { description.element, compiledDelegate });

                            void Write(object fieldValue)
                            {
                                if (writeDelegate != null)
                                {
                                    var value = this.value;
                                    writeDelegate.Invoke(ref value, fieldValue);
                                    this.value = value;
                                }
                            }
                        }
                        else
                            Debug.LogWarning($"Method not found: '{nameof(INotifyValueChangedExtensions.RegisterValueChangedCallback)}'.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        Debug.LogWarning("An exception occurred while registering a write event on an inner field of a composite field, and registration failed.");
                    }
                }

                return;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogWarning($"Registration failed with an exception while registering {description.elementType} type to the composite field.");
            }

            return;
        }
        
        
        
        /// <summary>
        /// 값 변경 알림 없이 새 값을 설정합니다. 이 메소드는 <see cref="UpdateDisplay"/>를 호출하여 UI를 업데이트합니다.
        /// </summary>
        /// <param name="newValue">설정할 새 값입니다.</param>
        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateDisplay();
        }

        /// <summary>
        /// 모든 자식 필드의 UI를 현재 <see cref="BaseField{TValueType}.rawValue"/>에 맞게 업데이트합니다.
        /// </summary>
        public void UpdateDisplay()
        {
            foreach (var description in descriptions)
            {
                if (description is IFieldDescription fieldDescription)
                    fieldDescription.displayEvent?.Invoke(rawValue);
            }
        }
        
        
        
        /// <summary>
        /// CSS에 의해 너비가 조정되는 빈 <see cref="VisualElement"/>를 반환합니다.
        /// </summary>
        /// <returns>스페이서 역할을 하는 <see cref="ElementDescription{TElement}"/>입니다.</returns>
        protected ElementDescription<VisualElement> GetSpacer()
        {
            VisualElement spacer = new VisualElement();
            
            spacer.AddToClassList(spacerUssClassName);
            spacer.visible = false;
            spacer.focusable = false;

            return new ElementDescription<VisualElement>(spacerUssClassName, spacer);
        }
        
        

        /// <summary>
        /// <see cref="RuniBaseCompositeField{TValueType}"/>를 구성하는 자식 요소의 속성을 기술하는 인터페이스입니다.
        /// </summary>
        public interface IElementDescription
        {
            /// <summary>
            /// 이 설명이 나타내는 요소의 타입입니다.
            /// </summary>
            Type elementType { get; }
            /// <summary>
            /// 이 설명이 나타내는 실제 <see cref="VisualElement"/> 인스턴스입니다.
            /// </summary>
            [DisallowNull] VisualElement? element { get; set; }
        }
        
        /// <summary>
        /// <see cref="RuniBaseCompositeField{TValueType}"/>를 구성하는 일반적인 <see cref="VisualElement"/>에 대한 설명 구조체입니다.
        /// </summary>
        /// <typeparam name="TElement">설명하는 요소의 타입입니다.</typeparam>
        public struct ElementDescription<TElement> : IElementDescription where TElement : VisualElement, new()
        {
            /// <summary>
            /// 이름만 사용하여 <see cref="ElementDescription{TElement}"/>의 새 인스턴스를 초기화합니다.
            /// </summary>
            /// <param name="name">요소의 이름입니다.</param>
            public ElementDescription(string name)
            {
                _element = null;
                _name = name;
            }
            
            /// <summary>
            /// 이름과 기존 요소 인스턴스를 사용하여 <see cref="ElementDescription{TElement}"/>의 새 인스턴스를 초기화합니다.
            /// </summary>
            /// <param name="name">요소의 이름입니다.</param>
            /// <param name="element">기존 요소 인스턴스입니다.</param>
            public ElementDescription(string name, TElement element)
            {
                _element = null;
                _name = name;
                
                this.element = element;
            }

            /// <inheritdoc/>
            public Type elementType => typeof(TElement);

            /// <summary>
            /// 이 설명이 나타내는 실제 <typeparamref name="TElement"/> 인스턴스입니다.
            /// </summary>
            [DisallowNull]
            public TElement? element
            {
                readonly get => _element;
                set
                {
                    _element = value;
                    _element.name = name;
                }
            }
            TElement? _element;

            /// <inheritdoc/>
            [DisallowNull]
            VisualElement? IElementDescription.element
            {
                get => element;
                set => element = (TElement)value;
            }

            /// <summary>
            /// 요소의 고유한 이름입니다.
            /// </summary>
            public readonly string name => _name ?? string.Empty;
            readonly string? _name;
        }
        
        /// <summary>
        /// <see cref="RuniBaseCompositeField{TValueType}"/>를 구성하는 자식 필드의 속성을 기술하는 인터페이스입니다.
        /// </summary>
        public interface IFieldDescription : IElementDescription
        {
            /// <summary>
            /// 필드가 나타내는 값의 타입입니다.
            /// </summary>
            Type fieldValueType { get; }
            
            /// <summary>
            /// 부모 필드의 값이 변경될 때 호출되는 이벤트입니다.
            /// </summary>
            Action<TValueType>? displayEvent { get; }
            /// <summary>
            /// 자식 필드의 값이 변경될 때 호출되는 이벤트입니다.
            /// </summary>
            WriteDelegate? writeEvent { get; }
            
            /// <summary>
            /// 필드 값을 쓰기 위한 델리게이트입니다.
            /// </summary>
            /// <param name="value">참조로 전달되는 부모 필드의 값입니다.</param>
            /// <param name="fieldValue">자식 필드의 새 값입니다.</param>
            delegate void WriteDelegate(ref TValueType value, object? fieldValue);
        }
        
        /// <summary>
        /// <see cref="RuniBaseCompositeField{TValueType}"/>를 구성하는 필드에 대한 설명 구조체입니다.
        /// </summary>
        /// <typeparam name="TField">설명하는 필드의 타입입니다.</typeparam>
        /// <typeparam name="TFieldValueType">설명하는 필드의 값 타입입니다.</typeparam>
        public struct FieldDescription<TField, TFieldValueType> : IFieldDescription where TField : VisualElement, INotifyValueChanged<TFieldValueType>, new()
        {
            /// <summary>
            /// 새로운 <see cref="FieldDescription{TField, TFieldValueType}"/> 인스턴스를 초기화합니다.
            /// </summary>
            /// <param name="propertyPath">필드의 바인딩 경로입니다.</param>
            /// <param name="displayEvent">부모 값이 변경될 때 호출되는 이벤트입니다.</param>
            /// <param name="writeEvent">자식 필드 값이 변경될 때 호출되는 이벤트입니다.</param>
            public FieldDescription(string propertyPath, ReadDelegate displayEvent, WriteDelegate writeEvent) : this(null, propertyPath, displayEvent, writeEvent) { }
            
            /// <summary>
            /// 새로운 <see cref="FieldDescription{TField, TFieldValueType}"/> 인스턴스를 초기화합니다.
            /// </summary>
            /// <param name="label">필드에 표시될 라벨입니다.</param>
            /// <param name="propertyPath">필드의 바인딩 경로입니다.</param>
            /// <param name="displayEvent">부모 값이 변경될 때 호출되는 이벤트입니다.</param>
            /// <param name="writeEvent">자식 필드 값이 변경될 때 호출되는 이벤트입니다.</param>
            public FieldDescription(string? label, string propertyPath, ReadDelegate displayEvent, WriteDelegate writeEvent)
            {
                this.label = label;
                _propertyPath = propertyPath;
                
                _field = null;

                this.displayEvent = displayEvent;
                this.writeEvent = writeEvent;

                internalDisplayEvent = null;
                internalWriteEvent = null;
            }
            
            /// <summary>
            /// 기존 필드 인스턴스를 사용하여 <see cref="FieldDescription{TField, TFieldValueType}"/>의 새 인스턴스를 초기화합니다.
            /// </summary>
            public FieldDescription(string propertyPath, TField field, ReadDelegate displayEvent, WriteDelegate writeEvent) : this(null, propertyPath, field, displayEvent, writeEvent) => this.field = field;
            
            /// <summary>
            /// 기존 필드 인스턴스를 사용하여 <see cref="FieldDescription{TField, TFieldValueType}"/>의 새 인스턴스를 초기화합니다.
            /// </summary>
            /// <param name="label">필드에 표시될 라벨입니다.</param>
            /// <param name="propertyPath">필드의 바인딩 경로입니다.</param>
            /// <param name="field">기존 필드 인스턴스입니다.</param>
            /// <param name="displayEvent">부모 값이 변경될 때 호출되는 이벤트입니다.</param>
            /// <param name="writeEvent">자식 필드 값이 변경될 때 호출되는 이벤트입니다.</param>
            public FieldDescription(string? label, string propertyPath, TField field, ReadDelegate displayEvent, WriteDelegate writeEvent) : this(label, propertyPath, displayEvent, writeEvent) => this.field = field;

            /// <summary>
            /// 부모 필드 값으로부터 자식 필드 값을 읽기 위한 델리게이트입니다.
            /// </summary>
            /// <param name="value">부모 필드의 현재 값입니다.</param>
            /// <returns>자식 필드에 표시될 값입니다.</returns>
            public delegate TFieldValueType ReadDelegate(TValueType value);
            /// <summary>
            /// 자식 필드 값을 부모 필드 값에 쓰기 위한 델리게이트입니다.
            /// </summary>
            /// <param name="value">참조로 전달되는 부모 필드의 값입니다.</param>
            /// <param name="fieldValue">자식 필드의 새 값입니다.</param>
            public delegate void WriteDelegate(ref TValueType value, TFieldValueType? fieldValue);

            /// <inheritdoc/>
            public Type elementType => typeof(TField);
            
            /// <inheritdoc/>
            public Type fieldValueType => typeof(TFieldValueType);

            /// <summary>
            /// 필드의 라벨입니다.
            /// </summary>
            public string? label { get; }

            /// <summary>
            /// 필드의 바인딩 경로입니다.
            /// </summary>
            public readonly string propertyPath => _propertyPath ?? string.Empty;
            readonly string? _propertyPath;

            /// <summary>
            /// 이 설명이 나타내는 실제 <typeparamref name="TField"/> 인스턴스입니다.
            /// </summary>
            [DisallowNull]
            public TField? field
            {
                readonly get => _field;
                set
                {
                    _field = value;

                    var thisClone = this;
                    if (value is BaseField<TFieldValueType> prefixLabel)
                        prefixLabel.label = label;
                    
                    value.name = $"unity-{propertyPath}-input"; // 유니티는 부모 바인딩 패치가 정상적이면 재귀적으로 자식을 찾아서 어쩌구 저쩌구 하기에 하여튼 이런식으로 직렬화된 프로퍼티 경로를 기준으로 이름을 짓지 않으면 프로퍼티로 인식 안함

                    internalDisplayEvent = x => value.SetValueWithoutNotify(thisClone.displayEvent.Invoke(x));
                    internalWriteEvent = (ref TValueType value, object? fieldValue) => thisClone.writeEvent.Invoke(ref value, fieldValue != null ? (TFieldValueType)fieldValue : default);
                }
            }
            TField? _field;
            
            /// <inheritdoc/>
            [DisallowNull]
            public VisualElement? element
            {
                readonly get => field;
                set => field = (TField)value;
            }

            /// <summary>
            /// 부모 필드의 값이 변경될 때 호출되는 이벤트입니다.
            /// </summary>
            public ReadDelegate displayEvent { get; }
            Action<TValueType>? internalDisplayEvent;
            /// <inheritdoc/>
            Action<TValueType>? IFieldDescription.displayEvent => internalDisplayEvent;
 
            /// <summary>
            /// 자식 필드의 값이 변경될 때 호출되는 이벤트입니다.
            /// </summary>
            public WriteDelegate writeEvent { get; }
            IFieldDescription.WriteDelegate? internalWriteEvent;
            IFieldDescription.WriteDelegate? IFieldDescription.writeEvent => internalWriteEvent;
        }
    }
}