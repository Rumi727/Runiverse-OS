#nullable enable
using Unity.Properties;
using UnityEngine.UIElements;

namespace RuniEngine.UI
{
    [UxmlElement]
    public partial class RuniProgressBar : BindableElement, INotifyValueChanged<float>
    {
        static readonly BindingId lowValueProperty = nameof(lowValue);
        static readonly BindingId highValueProperty = nameof(highValue);
        static readonly BindingId valueProperty = nameof(value);
        static readonly BindingId invertedProperty = nameof(inverted);



        public const string ussClassName = "runios-progress-bar";
        public const string containerUssClassName = ussClassName + "__container";
        public const string backgroundUssClassName = ussClassName + "__background";
        public const string viewportUssClassName = ussClassName + "__viewport";
        public const string progressUssClassName = ussClassName + "__progress";



        [UxmlAttribute]
        [CreateProperty]
        public float lowValue 
        { 
            get => _lowValue;
            set
            {
                if (_lowValue == value)
                    return;

                _lowValue = value;

                UpdateLayout();
                NotifyPropertyChanged(lowValueProperty);
            }
        }
        float _lowValue = 0;

        [UxmlAttribute]
        [CreateProperty]
        public float highValue
        {
            get => _highValue;
            set
            {
                if (_highValue == value)
                    return;

                _highValue = value;

                UpdateLayout();
                NotifyPropertyChanged(highValueProperty);
            }
        }
        float _highValue = 100;

        [UxmlAttribute]
        [CreateProperty]
        public float value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;

                if (panel != null)
                {
                    using var changeEvent = ChangeEvent<float>.GetPooled(_value, value);

                    SetValueWithoutNotify(value);
                    SendEvent(changeEvent);
                    NotifyPropertyChanged(valueProperty);

                    return;
                }

                SetValueWithoutNotify(value);
            }
        }
        float _value = 0;

        [UxmlAttribute]
        [CreateProperty]
        public bool inverted
        {
            get => _inverted;
            set
            {
                if (_inverted == value)
                    return;

                _inverted = value;

                UpdateLayout();
                NotifyPropertyChanged(invertedProperty);
            }
        }
        bool _inverted = false;



        public VisualElement container { get; }
        public VisualElement background { get; }
        public VisualElement viewport { get; }
        public VisualElement progress { get; }

        public RuniProgressBar()
        {
            AddToClassList(AbstractProgressBar.ussClassName);
            AddToClassList(ussClassName);

            container = new VisualElement { name = ussClassName };
            container.AddToClassList(AbstractProgressBar.containerUssClassName);
            container.AddToClassList(containerUssClassName);
            hierarchy.Add(container);

            background = new VisualElement() { name = backgroundUssClassName };
            background.AddToClassList(AbstractProgressBar.backgroundUssClassName);
            background.AddToClassList(backgroundUssClassName);
            container.Add(background);

            viewport = new VisualElement() { name = viewportUssClassName };
            viewport.AddToClassList(viewportUssClassName);
            background.Add(viewport);

            progress = new VisualElement() { name = progressUssClassName };
            progress.AddToClassList(AbstractProgressBar.progressUssClassName);
            progress.AddToClassList(progressUssClassName);
            viewport.Add(progress);

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        void OnGeometryChanged(GeometryChangedEvent e) => UpdateLayout();



        public virtual void UpdateLayout()
        {
            float normalizedProgress = GetNormalizedProgress();

            viewport.style.position = Position.Absolute;
            viewport.style.flexDirection = inverted ? FlexDirection.RowReverse : FlexDirection.Row;

            viewport.style.left = inverted ? (background.resolvedStyle.width * (1 - normalizedProgress)) : 0;
            viewport.style.right = inverted ? 0 : (background.resolvedStyle.width * (1 - normalizedProgress));
            viewport.style.bottom = 0;
            viewport.style.top = 0;
            
            progress.style.left = 0;
            progress.style.right = 0;
            progress.style.bottom = 0;
            progress.style.top = 0;
        }

        public virtual float GetNormalizedProgress() => MathUtility.InverseLerp(lowValue, highValue, value);

        public virtual void SetValueWithoutNotify(float newValue)
        {
            _value = newValue;
            UpdateLayout();
        }
    }
}
