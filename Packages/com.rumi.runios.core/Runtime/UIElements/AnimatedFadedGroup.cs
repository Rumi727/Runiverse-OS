#nullable enable
using RuniOS.AnimatedValues;
using System;
using Unity.Properties;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    [UxmlElement]
    public partial class AnimatedFadedGroup : BindableElement, INotifyValueChanged<bool>
    {
        public const string ussClassName = "runios-animated-faded-group";
        public const string viewportClippingUssClassName = "runios-animated-faded-group__viewport-clipping";
        public const string viewportUssClassName = "runios-animated-faded-group__viewport";
        public const string contentUssClassName = "runios-animated-faded-group__content";
        
        public static readonly BindingId valueProperty = nameof(value);
        public static readonly BindingId directionProperty = nameof(direction);
        
        public static readonly CustomStyleProperty<string> easingStyleProperty = new CustomStyleProperty<string>("--runios-animated_faded_group-easing");
        public static readonly CustomStyleProperty<float> durationStyleProperty = new CustomStyleProperty<float>("--runios-animated_faded_group-duration");
        
        [UxmlAttribute]
        [CreateProperty]
        public bool value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;
                
                SetValueWithoutNotify(value);
                
                using ChangeEvent<bool> pooled = ChangeEvent<bool>.GetPooled(_value, value);
                pooled.target = this;
                SendEvent(pooled);
                
                NotifyPropertyChanged(in valueProperty);
            }
        }
        bool _value = false;
        
        [UxmlAttribute]
        [CreateProperty]
        public Direction direction
        {
            get => _direction;
            set
            {
                if (_direction == value)
                    return;

                _direction = value;
                NotifyPropertyChanged(in directionProperty);
                
                Update();
            }
        }
        Direction _direction = Direction.vertical;

        public EasingFunction.Ease easing => easingStyle.keyword != StyleKeyword.Null ? easingStyle.value : _easing;
        EasingFunction.Ease _easing = EasingFunction.Ease.Linear;
        
        public float duration => durationStyle.keyword != StyleKeyword.Null ? durationStyle.value : _duration;
        float _duration = 0;

        public StyleEnum<EasingFunction.Ease> easingStyle
        {
            get => _easingStyle;
            set
            {
                _easingStyle = value;
                animBool.easing = easing;
                
                Update();
            }
        }
        StyleEnum<EasingFunction.Ease> _easingStyle = StyleKeyword.Null;

        public StyleFloat durationStyle
        {
            get => _durationStyle;
            set
            {
                _durationStyle = value;
                animBool.duration = duration;
                
                Update();
            }
        }
        StyleFloat _durationStyle = StyleKeyword.Null;

        public VisualElement viewportClipping { get; }
        public VisualElement viewport { get; }
        public override VisualElement contentContainer { get; }



        readonly AnimBool animBool;
        readonly IVisualElementScheduledItem scheduledItem;

        public AnimatedFadedGroup() : this(false) { }
        public AnimatedFadedGroup(bool value) : this(value, new VisualElement()) { }
        public AnimatedFadedGroup(VisualElement contentContainer) : this(false, contentContainer) { }
        public AnimatedFadedGroup(bool value, VisualElement contentContainer)
        {
            AddToClassList(ussClassName);
            
            styleSheets.Insert(0, UIToolkitUtility.rosControlStyle);
            
            viewportClipping = new VisualElement { name = viewportClippingUssClassName };
            viewportClipping.AddToClassList(viewportClippingUssClassName);
            hierarchy.Add(viewportClipping);
            
            viewport = new VisualElement { name = viewportUssClassName };
            viewport.AddToClassList(viewportUssClassName);
            viewportClipping.hierarchy.Add(viewport);
            
            this.contentContainer = contentContainer;
            contentContainer.AddToClassList(contentUssClassName);
            viewport.hierarchy.Add(contentContainer);
            
            RegisterCallback<AttachToPanelEvent>(_ => Update());
            RegisterCallback<CustomStyleResolvedEvent>(CustomStyleResolvedEventCallback);
            contentContainer.RegisterCallback<GeometryChangedEvent>(_ => Update());

            _value = value;
            
            animBool = new AnimBool(value);
            animBool.onAnimationBegin += OnAnimationBegin;
            scheduledItem = viewportClipping.schedule.Execute(Update).Every(0);
            scheduledItem.Pause();
            animBool.onAnimationEnd += OnAnimationEnd;
        }

        void OnAnimationBegin() => scheduledItem.Resume();

        void Update()
        {
            if (panel == null)
                return;

            float width = this.RoundToPanelPixelSize(contentContainer.resolvedStyle.width * animBool.value);
            float height = this.RoundToPanelPixelSize(contentContainer.resolvedStyle.height * animBool.value);
            
            if (value)
                contentContainer.style.display = DisplayStyle.Flex;
            else
            {
                if ((direction == Direction.horizontal && width <= 1) || (direction == Direction.vertical && height <= 1))
                    contentContainer.style.display = DisplayStyle.None;
            }

            if (direction == Direction.horizontal)
            {
                viewportClipping.style.width = width;
                viewportClipping.style.height = StyleKeyword.Null;
            }
            else
            {
                viewportClipping.style.width = StyleKeyword.Null;
                viewportClipping.style.height = height;
            }
        }

        void OnAnimationEnd()
        {
            scheduledItem.Pause();
            Update();
        }

        void CustomStyleResolvedEventCallback(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(easingStyleProperty, out string easingStyle) && Enum.TryParse(easingStyle, true, out EasingFunction.Ease easing))
            {
                _easing = easing;
                animBool.easing = this.easing;
            }

            if (evt.customStyle.TryGetValue(durationStyleProperty, out float duration))
            {
                _duration = duration;
                animBool.duration = this.duration;
            }
        }

        public void SetValueWithoutNotify(bool newValue)
        {
            _value = newValue;
            SetCheckedPseudoState(newValue);
            
            animBool.target = newValue;
        }
        
        public enum Direction
        {
            horizontal,
            vertical
        }
    }
}
