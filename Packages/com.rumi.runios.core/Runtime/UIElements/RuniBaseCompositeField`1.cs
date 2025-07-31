#nullable enable
using System;
using System.Collections.Generic;
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
        public const string twoLinesVariantUssClassName = ussClassName + "--two-lines";
        
        public VisualElement visualInput { get; }

        public IReadOnlyList<IElementDescription> descriptions { get; }

        protected RuniBaseCompositeField(int fieldsByLine) : this(null, fieldsByLine) { }
        protected RuniBaseCompositeField(string? label, int fieldsByLine) : base(label, new VisualElement())
        {
            AddToClassList(ussClassName);
            delegatesFocus = false;
            
            labelElement.AddToClassList(labelUssClassName);
            
            visualInput = this.Q<VisualElement>(className: BaseField<TValueType>.inputUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            visualInput.focusable = false;
            
            // ReSharper disable once VirtualMemberCallInConstructor
            descriptions = DescribeFields().ToArray().AsReadOnly();
            
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
                for (int j = i * fieldsByLine; j < ((i * fieldsByLine) + fieldsByLine).Min(descriptions.Count); j++)
                {
                    IElementDescription description = descriptions[j];
                    VisualElement? element = description.element;
                    if (element == null)
                        continue;

                    element.delegatesFocus = true;

                    if (description is IFieldDescription fieldDescription && element.GetType().IsAssignableToGenericDefinition(typeof(BaseField<>), out Type? fieldType))
                    {
                        element.AddToClassList(fieldUssClassName);
                        if (isFirst)
                            element.AddToClassList(firstFieldVariantUssClassName);

                        isFirst = false;

                        try
                        {
                            MethodInfo? changedCallback = AccessUtility.DeclaredMethod(typeof(INotifyValueChangedExtensions), nameof(INotifyValueChangedExtensions.RegisterValueChangedCallback));
                            if (changedCallback != null)
                            {
                                Type fieldValueType = fieldType.GenericTypeArguments[0];
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
                                changedCallback.Invoke(description.element, new object[] { element, compiledDelegate });

                                void Write(object fieldValue)
                                {
                                    var value = this.value;
                                    fieldDescription.writeEvent?.Invoke(ref value, fieldValue);
                                    this.value = value;
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
                    
                    hierarchy.Add(element);
                }
            }

            UpdateDisplay();
        }

        /// <summary>
        /// 이 메소드는 자식 등록을 위해 생성자에서 호출됩니다.
        /// </summary>
        protected abstract IEnumerable<IElementDescription> DescribeFields();
        
        

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
        
        
        
        protected ElementDescription GetSpacer()
        {
            VisualElement spacer = new VisualElement();
            
            spacer.AddToClassList(spacerUssClassName);
            spacer.visible = false;
            spacer.focusable = false;

            return new ElementDescription(spacerUssClassName, spacer);
        }
        
        

        public interface IElementDescription
        {
            VisualElement? element { get; }
        }
        
        public readonly struct ElementDescription : IElementDescription
        {
            public ElementDescription(string propertyPath, VisualElement element)
            {
                this.propertyPath = propertyPath;
                this.element = element;
                
                element.name = propertyPath;
            }
            
            public string? propertyPath { get; }
            
            public VisualElement? element { get; }
        }
        
        public interface IFieldDescription : IElementDescription
        {
            Action<TValueType>? displayEvent { get; }
            WriteDelegate? writeEvent { get; }
            
            delegate void WriteDelegate(ref TValueType value, object? fieldValue);
        }
        
        public readonly struct FieldDescription<TField, TFieldType> : IFieldDescription where TField : BaseField<TFieldType>, new()
        {
            public FieldDescription(string label, string propertyPath, ReadDelegate displayEvent, WriteDelegate writeEvent) : this(label, propertyPath, new TField(), displayEvent, writeEvent) { }
            public FieldDescription(string label, string propertyPath, TField field, ReadDelegate displayEvent, WriteDelegate writeEvent)
            {
                this.label = label;
                this.propertyPath = propertyPath;

                this.field = field;

                this.displayEvent = displayEvent;
                internalDisplayEvent = x => field.SetValueWithoutNotify(displayEvent.Invoke(x));
                
                this.writeEvent = writeEvent;
                internalWriteEvent = (ref TValueType value, object? fieldValue) => writeEvent.Invoke(ref value, fieldValue != null ? (TFieldType)fieldValue : default);

                field.label = label;
                field.name = $"unity-{propertyPath}-input"; // 유니티는 부모 바인딩 패치가 정상적이면 재귀적으로 자식을 찾아서 어쩌구 저쩌구 하기에 하여튼 이런식으로 직렬화된 프로퍼티 경로를 기준으로 이름을 짓지 않으면 프로퍼티로 인식 안함
            }

            public delegate TFieldType ReadDelegate(TValueType value);
            public delegate void WriteDelegate(ref TValueType value, TFieldType? fieldValue);
            
            public string? label { get; }
            public string? propertyPath { get; }
            
            public BaseField<TFieldType>? field { get; }
            VisualElement? IElementDescription.element => field;
            
            public ReadDelegate? displayEvent { get; }
            readonly Action<TValueType>? internalDisplayEvent;
            Action<TValueType>? IFieldDescription.displayEvent => internalDisplayEvent;
            
            public WriteDelegate? writeEvent { get; }
            readonly IFieldDescription.WriteDelegate? internalWriteEvent;
            IFieldDescription.WriteDelegate? IFieldDescription.writeEvent => internalWriteEvent;
        }
    }
}
