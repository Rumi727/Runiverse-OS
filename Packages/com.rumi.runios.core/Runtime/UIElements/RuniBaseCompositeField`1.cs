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
    public abstract class RuniBaseCompositeField<TValueType> : BaseField<TValueType>
    {
        public new const string ussClassName = "unity-composite-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        public const string spacerUssClassName = ussClassName + "__field-spacer";
        public const string multilineVariantUssClassName = ussClassName + "--multi-line";
        public const string fieldGroupUssClassName = ussClassName + "__field-group";
        public const string fieldUssClassName = ussClassName + "__field";
        public const string firstFieldVariantUssClassName = fieldUssClassName + "--first";
        public const string lastFieldVariantUssClassName = fieldUssClassName + "--last";
        public const string twoLinesVariantUssClassName = ussClassName + "--two-lines";
        
        public VisualElement visualInput { get; }

        public IReadOnlyList<IElementDescription> descriptions => _descriptions ??= GetElementDescriptions().ToArray().AsReadOnly();
        IReadOnlyList<IElementDescription>? _descriptions;
        
        protected RuniBaseCompositeField(string? label) : base(label, new VisualElement())
        {
            AddToClassList(ussClassName);
            delegatesFocus = false;
            
            labelElement.AddToClassList(labelUssClassName);
            
            visualInput = this.Q<VisualElement>(className: BaseField<TValueType>.inputUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.focusable = false;
        }
        
        protected abstract IEnumerable<IElementDescription> GetElementDescriptions();

        public void SetFieldsByLine(int fieldsByLine)
        {
            visualInput.Clear();
            
            int line = 1;
            if (fieldsByLine > 1)
                line = ((float)descriptions.Count / fieldsByLine).CeilToInt();
            
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
                    InitializeElement(description);
                    
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
                        if (j >= lastIndex)
                            element.AddToClassList(lastFieldVariantUssClassName);
                    }
                    
                    hierarchy.Add(element);
                }
            }

            UpdateDisplay();
        }

        public void InitializeElement(IElementDescription description)
        {
            if (description.element != null)
                return;
            
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

                description.element = (VisualElement)Activator.CreateInstance(description.elementType);
                description.element.delegatesFocus = true;

                if (description is IFieldDescription fieldDescription && fieldDescription.writeEvent != null && typeof(INotifyValueChanged<>).MakeGenericType(fieldDescription.fieldValueType).IsInstanceOfType(element))
                {
                    description.element.AddToClassList(fieldUssClassName);

                    try
                    {
                        MethodInfo? changedCallback = AccessUtility.DeclaredMethod(typeof(INotifyValueChangedExtensions), nameof(INotifyValueChangedExtensions.RegisterValueChangedCallback));
                        if (changedCallback != null)
                        {
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
                            changedCallback.Invoke(null, new object[]
                            {
                                description.element, compiledDelegate
                            });

                            void Write(object fieldValue)
                            {
                                if (fieldDescription.writeEvent != null)
                                {
                                    var value = this.value;
                                    fieldDescription.writeEvent.Invoke(ref value, fieldValue);
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
        
        
        
        public override void SetValueWithoutNotify(TValueType newValue)
        {
            base.SetValueWithoutNotify(newValue);
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            foreach (var description in descriptions)
            {
                if (description is IFieldDescription fieldDescription)
                    fieldDescription.displayEvent?.Invoke(rawValue);
            }
        }
        
        
        
        protected ElementDescription<VisualElement> GetSpacer()
        {
            VisualElement spacer = new VisualElement();
            
            spacer.AddToClassList(spacerUssClassName);
            spacer.visible = false;
            spacer.focusable = false;

            return new ElementDescription<VisualElement>(spacerUssClassName, spacer);
        }
        
        

        public interface IElementDescription
        {
            Type elementType { get; }
            [DisallowNull] VisualElement? element { get; set; }
        }
        
        public struct ElementDescription<TElement> : IElementDescription where TElement : VisualElement, new()
        {
            public ElementDescription(string name)
            {
                _element = null;
                _name = name;
            }
            
            public ElementDescription(string name, TElement element)
            {
                _element = null;
                _name = name;
                
                this.element = element;
            }

            public Type elementType => typeof(TElement);

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

            [DisallowNull]
            VisualElement? IElementDescription.element
            {
                get => element;
                set => element = (TElement)value;
            }

            public readonly string name => _name ?? string.Empty;
            readonly string? _name;
        }
        
        public interface IFieldDescription : IElementDescription
        {
            Type fieldValueType { get; }
            
            Action<TValueType>? displayEvent { get; }
            WriteDelegate? writeEvent { get; }
            
            delegate void WriteDelegate(ref TValueType value, object? fieldValue);
        }
        
        public struct FieldDescription<TField, TFieldValueType> : IFieldDescription where TField : VisualElement, INotifyValueChanged<TFieldValueType>, new()
        {
            public FieldDescription(string label, string propertyPath, ReadDelegate displayEvent, WriteDelegate writeEvent)
            {
                _label = label;
                _propertyPath = propertyPath;
                
                _field = null;

                this.displayEvent = displayEvent;
                this.writeEvent = writeEvent;

                internalDisplayEvent = null;
                internalWriteEvent = null;
            }
            public FieldDescription(string label, string propertyPath, TField field, ReadDelegate displayEvent, WriteDelegate writeEvent) : this(label, propertyPath, displayEvent, writeEvent) => this.field = field;

            public delegate TFieldValueType ReadDelegate(TValueType value);
            public delegate void WriteDelegate(ref TValueType value, TFieldValueType? fieldValue);

            public Type elementType => typeof(TField);
            public Type fieldValueType => typeof(TFieldValueType);

            public readonly string label => _label ?? string.Empty;
            readonly string? _label;

            public readonly string propertyPath => _propertyPath ?? string.Empty;
            readonly string? _propertyPath;

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
            
            [DisallowNull]
            public VisualElement? element
            {
                readonly get => field;
                set => field = (TField)value;
            }

            public ReadDelegate displayEvent { get; }
            Action<TValueType>? internalDisplayEvent;
            Action<TValueType>? IFieldDescription.displayEvent => internalDisplayEvent;
            
            public WriteDelegate writeEvent { get; }
            IFieldDescription.WriteDelegate? internalWriteEvent;
            IFieldDescription.WriteDelegate? IFieldDescription.writeEvent => internalWriteEvent;
        }
    }
}
