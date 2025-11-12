#nullable enable
using RuniOS.Localizations;
using System;
using Unity.Properties;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements
{
    [UxmlElement]
    public partial class TypeField : BaseField<SerializableType>
    {
        public new const string ussClassName = "runios-type-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public const string textUssClassName = ussClassName + "__text";

        public const string buttonUssClassName = ussClassName + "__button";



        public override SerializableType value
        {
            get => base.value;
            set
            {
                if (base.value != value)
                {
                    if (value != null && (baseType == null || baseType.IsAssignableFrom(value)))
                        base.value = value;
                    else
                        base.value = null;
                }
            }
        }

#pragma warning disable UITKSG015
        [CreateProperty]
        [UxmlAttribute("base-type")]
        public Type? baseType
        {
            get => _baseType;
            set
            {
                _baseType = value;
                if (this.value != null && (_baseType != null && !_baseType.IsAssignableFrom(this.value)))
                    this.value = null;

                NotifyPropertyChanged(baseTypeProperty);
            }
        }
        Type? _baseType = null;
        static readonly BindingId baseTypeProperty = nameof(baseType);
#pragma warning restore UITKSG015

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



        public VisualElement visualInput { get; }
        
        public TextElement textElement { get; }
        public Button buttonElement { get; }



        public TypeField() : this(string.Empty) { }
        public TypeField(Type? baseType) : this(string.Empty, baseType) { }
        public TypeField(string label, Type? baseType = null) : base(label, new VisualElement())
        {
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            labelElement.AddToClassList(labelUssClassName);

            AddToClassList(ussClassName);
            visualInput = this.Q<VisualElement>(className: BaseField<SerializableType>.inputUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            
            textElement = new TextElement { name = textUssClassName, pickingMode = PickingMode.Ignore };
            textElement.AddToClassList(textUssClassName);
            visualInput.Add(textElement);
            
            buttonElement = new Button(ShowSelector) { name = buttonUssClassName };
            buttonElement.AddToClassList(buttonUssClassName);
            visualInput.Add(buttonElement);
            
            this.baseType = baseType;
            
#if UNITY_EDITOR
            RegisterCallback<DetachFromPanelEvent>(_ => EditorLocalizationBridge.onLanguageUpdate -= UpdateButtonText);
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                if (this.IsEditorPanel())
                    EditorLocalizationBridge.onLanguageUpdate += UpdateButtonText;
                
                UpdateLabel();
                UpdateButtonText();
            });
#endif
        }

        //런타임 지원 예정
        public void ShowSelector()
        {
#if UNITY_EDITOR
            if (this.IsEditorPanel())
            {
                var provider = APIBridge.UnityEditor.UIElements.TypeSearchProviderBridge.__CreateInstanceNonPublic(baseType ?? typeof(object));
                var context = UnityEditor.Search.SearchService.CreateContext(provider.__instance, "type:");
                var viewState = new UnityEditor.Search.SearchViewState(context)
                {
                    title = "Type",
                    queryBuilderEnabled = true,
                    hideTabs = true,
                    selectHandler = Select,
                    flags = (UnityEngine.Search.SearchViewFlags.TableView | UnityEngine.Search.SearchViewFlags.DisableInspectorPreview | UnityEngine.Search.SearchViewFlags.DisableBuilderModeToggle)
                };
                UnityEditor.Search.SearchService.ShowPicker(viewState);

                return;
            }
#endif
        }

#if UNITY_EDITOR
        void Select(UnityEditor.Search.SearchItem? item, bool cancelled)
        {
            if (item?.data is Type type)
                value = type;
            else
                value = null;
        }
#endif

        public override void SetValueWithoutNotify(SerializableType newValue)
        {
            base.SetValueWithoutNotify(newValue);
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
            if (value.value == null)
            {
#if UNITY_EDITOR
                if (this.IsEditorPanel())
                    ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify(EditorLocalizationBridge.GetTextOrKey("gui.none"));
                else
#endif
                    ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify("None");
                
                return;
            }
            
            if (displayFullName)
                ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify(value.value.SerializeToString());
            else
                ((INotifyValueChanged<string>)textElement).SetValueWithoutNotify(value.value.GetTypeDisplayName());
        }

        void UpdateButtonText()
        {
#if UNITY_EDITOR
            if (this.IsEditorPanel())
                buttonElement.text = EditorLocalizationBridge.GetTextOrKey("gui.type_field.select_type");
            else
#endif
                buttonElement.text = "Select Type...";
        }
    }
}
