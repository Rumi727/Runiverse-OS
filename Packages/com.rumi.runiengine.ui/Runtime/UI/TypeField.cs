#nullable enable
using System;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

namespace RuniEngine.UI
{
    [UxmlElement]
    public partial class TypeField : BaseField<string>
    {
        public APIBridge.UnityEngine.UIElements.BaseField<string> apiBridge { get; }



        public new const string ussClassName = "runios-type-field";

        public const string textUssClassName = ussClassName + "__text";

        public const string buttonUssClassName = ussClassName + "__button";



        public override string value
        {
            get => base.value ?? string.Empty;
            set
            {
                if (base.value != value)
                {
                    Type? valueAsType = TypeUtility.DeserializeFromString(value);
                    if (valueAsType != null && (baseType == null || baseType.IsAssignableFrom(valueAsType)))
                        base.value = valueAsType.SerializeToString();
                    else
                        base.value = string.Empty;
                }
            }
        }


#pragma warning disable CS8601 // 가능한 null 참조 할당입니다.
#pragma warning disable CS8603 // 가능한 null 참조 반환입니다.
        public Type? valueAsType
        {
            get => nullableValueAsType;
            set => nullableValueAsType = value;
        }
        Type? cachedType = null;

        [CreateProperty]
        [UxmlAttribute("value")]
        Type nullableValueAsType
        {
            get
            {
                if (cachedType != null)
                    return cachedType;
                else
                    return string.IsNullOrEmpty(value) ? null : TypeUtility.DeserializeFromString(value);
            }
            set
            {
                this.value = value?.SerializeToString() ?? string.Empty;
                NotifyPropertyChanged(valueProperty);
            }
        }
        static readonly BindingId valueProperty = nameof(nullableValueAsType);



        public Type? baseType
        {
            get => nullableBaseType;
            set => nullableBaseType = value;
        }

        //아니 미친 nullable 왜 지원 안함 유니티 병신 오류가 아니라 경고로라도 바꿔주던가 시ㅣ발
        [CreateProperty]
        [UxmlAttribute("base-type")]
        Type nullableBaseType
        {
            get => _baseType;
            set
            {
                _baseType = value;
                if (valueAsType == null || (_baseType != null && !_baseType.IsAssignableFrom(valueAsType)))
                    this.value = string.Empty;

                NotifyPropertyChanged(baseTypeProperty);
            }
        }
        Type? _baseType = null;
        static readonly BindingId baseTypeProperty = nameof(nullableBaseType);
#pragma warning restore CS8603 // 가능한 null 참조 반환입니다.
#pragma warning restore CS8601 // 가능한 null 참조 할당입니다.

        [UxmlAttribute]
        [CreateProperty]
        public bool displayFullName
        {
            get => _displayFullName;
            set
            {
                if (_displayFullName != value)
                {
                    _displayFullName = value;

                    UpdateLabel();
                    NotifyPropertyChanged(displayFullNameProperty);
                }
            }
        }
        bool _displayFullName = true;
        static readonly BindingId displayFullNameProperty = nameof(displayFullName);



        public Label textElement { get; }
        public Button buttonElement { get; }



        public TypeField() : this(string.Empty, null) { }
        public TypeField(string label, Type? baseType = null) : base(label, null)
        {
            apiBridge = APIBridge.UnityEngine.UIElements.BaseField<string>.GetInstance(this);
            AddToClassList(ussClassName);

            textElement = new Label { name = textUssClassName, pickingMode = PickingMode.Ignore };
            textElement.AddToClassList(textUssClassName);
            apiBridge.visualInput.Add(textElement);

            buttonElement = new Button(ShowSelector) { name = buttonUssClassName, text = "Select Type..." };
            buttonElement.AddToClassList(buttonUssClassName);
            apiBridge.visualInput.Add(buttonElement);
            
            this.baseType = baseType;
        }

        //런타임 지원 예정
        public void ShowSelector()
        {
#if UNITY_EDITOR
            if (this.IsEditorPanel())
            {
                var provider = Editor.APIBridge.UnityEditor.UIElements.TypeSearchProvider.CreateInstance(baseType ?? typeof(object));
                var context = UnityEditor.Search.SearchService.CreateContext(provider.instance, "type:");
                var viewState = new UnityEditor.Search.SearchViewState(context)
                {
                    title = "Type",
                    queryBuilderEnabled = true,
                    hideTabs = true,
                    selectHandler = Select,
                    flags = (SearchViewFlags.TableView | SearchViewFlags.DisableInspectorPreview | SearchViewFlags.DisableBuilderModeToggle)
                };
                UnityEditor.Search.SearchService.ShowPicker(viewState);

                return;
            }
#endif
        }

#if UNITY_EDITOR
        void Select(UnityEditor.Search.SearchItem item, bool cancelled)
        {
            if (item.data is Type type)
                value = type.SerializeToString();
            else
                value = string.Empty;
        }
#endif

        public override void SetValueWithoutNotify(string newValue)
        {
            base.SetValueWithoutNotify(newValue);
            
            cachedType = null;
            UpdateLabel();
        }

        protected override void UpdateMixedValueContent()
        {
            if (showMixedValue)
                ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify(mixedValueString);
            else
                UpdateLabel();
        }

        void UpdateLabel()
        {
            if (displayFullName)
            {
                if (string.IsNullOrEmpty(value))
                    ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify("None");
                else
                    ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify(value);
            }
            else
            {
                if (valueAsType == null)
                    ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify("None");
                else
                    ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify(valueAsType.GetTypeDisplayName());
            }
        }
    }
}
