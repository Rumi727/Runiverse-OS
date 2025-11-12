#nullable enable
using RuniOS.APIBridge.UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Nullables
{
    /// <summary>
    /// Nullable 쌍을 나타내는 복합 필드입니다.
    /// </summary>
    /// <typeparam name="TValueType">Nullable 타입이 나타내는 타입입니다.</typeparam>
    public class NullableField<TValueType> : RuniBaseCompositeField<SerializableNullable<TValueType>> where TValueType : struct
    {
        public new const string ussClassName = "runios-nullable-field";
        public const string nonTextFieldUssClassName = ussClassName + "--non-text-field";
        public const string textFieldUssClassName = ussClassName + "--text-field";
        public const string isNullUssClassName = ussClassName + "--is-null";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        public new const string fieldUssClassName = ussClassName + "__field";
        public const string nullLabelFieldUssClassName = ussClassName + "__null-label-field";
        public const string fieldLabelUssClassName = ussClassName + "__field__label";
        public const string toggleUssClassName = ussClassName + "__toggle";

        /// <summary>
        /// 필드에 대한 설명을 담고 있는 <see cref="RuniBaseCompositeField{TValueType}.IElementDescription"/>입니다.
        /// </summary>
        public IElementDescription description { get; }
        
        /// <summary>
        /// 토글 필드입니다.
        /// </summary>
        public Toggle toggle { get; }
        
        /// <summary>
        /// null 값일 때 표시할 텍스트를 결정합니다. null 값이면 기본 값을 사용합니다.
        /// </summary>
        public virtual string? nullText
        {
            get => _nullText;
            set
            {
                if (_nullText == value)
                    return;
                
                _nullText = value;
                Update();
            }
        }
        string? _nullText = null;
        
        /// <summary>
        /// null 값일 때 표시할 텍스트 요소입니다.
        /// </summary>
        public LabelField nullLabelField { get; }

        public TextInputBaseField<TValueType>? textInputFieldElement { get; private set; }
        public TextElement? textInputFieldTextElement { get; private set; }
        
        /// <summary>
        /// <see cref="NullableField{TNullable}"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="description">필드에 대한 설명 데이터입니다.</param>
        public NullableField(IElementDescription description) : this(string.Empty, description) { }
        
        /// <summary>
        /// <see cref="NullableField{TNullable}"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="description">필드에 대한 설명 데이터입니다.</param>
        /// <param name="nullText">null 값일 때 텍스트 필드에 표시할 텍스트를 결정합니다. null 값이면 기본 값을 사용합니다.</param>
        public NullableField(IElementDescription description, string? nullText = null) : this(string.Empty, description, nullText) { }
            
        /// <summary>
        /// <see cref="NullableField{TNullable}"/>의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="label">필드의 라벨입니다.</param>
        /// <param name="description">필드에 대한 설명 데이터입니다.</param>
        /// <param name="nullText">null 값일 때 텍스트 필드에 표시할 텍스트를 결정합니다. null 값이면 기본 값을 사용합니다.</param>
        public NullableField(string label, IElementDescription description, string? nullText = null) : base(string.Empty, CompositeConfig.compositedField)
        {
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            
            nullLabelField = new LabelField(label);
            nullLabelField.AddToClassList(nullLabelFieldUssClassName);
            
            this.description = description;
            description.element.AddToClassList(fieldUssClassName);

            toggle = new Toggle();
            toggle.AddToClassList(toggleUssClassName);

            this.RegisterLabelChangedCallback(SetLabel);
            
            SetFieldsByHorizontal();

            //인스펙터 요소는 ObjectDrawer 구현체 특성상 Foldout이 한번 방어해줌
            textInputFieldElement = description.element.Q<TextInputBaseField<TValueType>>(); 
            if (textInputFieldElement != null)
                RegisterTextInputElement();
            else
            {
                AddToClassList(nonTextFieldUssClassName);
                description.element.RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
            }

            _nullText = nullText;
            
            SetLabel(label);
            Update();
            
            return;

            void RegisterTextInputElement()
            {
                AddToClassList(textFieldUssClassName);
                RemoveFromClassList(nonTextFieldUssClassName);

                textInputFieldTextElement = TextInputBaseFieldBridge<TValueType>.__GetInstanceFrom(textInputFieldElement).m_TextInputBase.textElement;
                    
                textInputFieldElement.RegisterValueChangedCallback(_ => Update());
                textInputFieldTextElement.RegisterValueChangedCallback(_ => Update());
                
                textInputFieldElement.hierarchy.Add(toggle);
                Update();
            }

            void GeometryChangedCallback(GeometryChangedEvent _)
            {
                textInputFieldElement = description.element.Q<TextInputBaseField<TValueType>>();
                if (textInputFieldElement != null)
                {
                    RegisterTextInputElement();
                    UnregisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
                }

                Update();
            }
        }

        protected override IEnumerable<IElementDescription> GetElementDescriptions()
        {
            yield return description;
            yield return new ElementDescription<LabelField>("null-text", nullLabelField);
            yield return new FieldDescription<Toggle, bool>
            (
                SerializableNullable.nameOfInternalHasValue,
                toggle,
                static x => x != null,
                static (ref SerializableNullable<TValueType> value, bool hasValue) =>
                {
                    if (hasValue)
                    {
                        if (value == null) 
                            value = new TValueType();
                    }
                    else
                        value = null;
                }
            );
        }

        public override void SetValueWithoutNotify(SerializableNullable<TValueType> newValue)
        {
            base.SetValueWithoutNotify(newValue);
            Update();
        }

        void Update()
        {
            nullLabelField.value = nullText ?? $"null ({typeof(TValueType).GetTypeDisplayName()})";
            EnableInClassList(isNullUssClassName, value == null);
            
            if (textInputFieldTextElement != null)
            {
                textInputFieldTextElement.enabledSelf = value != null;
                
                if (value == null)
                    textInputFieldTextElement.SetValueWithoutNotify(nullLabelField.value);
                else
                    textInputFieldTextElement.SetValueWithoutNotify(value.Value.ToString());
            }
        }
        
        void SetLabel(string? label)
        {
            nullLabelField.label = label;

            if (IPrefixLabelBridge.__targetType.IsInstanceOfType(description.element))
            {
                IPrefixLabelBridge prefixLabel = IPrefixLabelBridge.__GetInstanceFrom(description.element);

                prefixLabel.SetLabel(label);
                prefixLabel.labelElement.AddToClassList(fieldLabelUssClassName);
            }
            else if (description.element is Foldout foldout)
                foldout.text = label;
#if UNITY_EDITOR
            else if (description.element is UnityEditor.UIElements.PropertyField propertyField)
                propertyField.label = label;
#endif
        }
    }
}
