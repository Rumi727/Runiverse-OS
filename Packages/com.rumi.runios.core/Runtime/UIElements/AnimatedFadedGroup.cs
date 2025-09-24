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
        public const string horizontalUssClassName = ussClassName + "__horizontal";
        public const string verticalUssClassName = ussClassName + "__vertical";
        public const string viewportClippingUssClassName = ussClassName + "__viewport-clipping";
        public const string viewportUssClassName = ussClassName + "__viewport";
        public const string contentUssClassName = ussClassName + "__content";
        
        public static readonly BindingId valueProperty = nameof(value);
        public static readonly BindingId sizeProperty = nameof(size);
        public static readonly BindingId maxSizeProperty = nameof(maxSize);
        public static readonly BindingId directionProperty = nameof(direction);
        public static readonly BindingId viewportSizeChangeProperty = nameof(viewportSizeChange);
        
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
                
                EnableInClassList(horizontalUssClassName, direction == Direction.horizontal);
                EnableInClassList(verticalUssClassName, direction == Direction.vertical);
            }
        }
        Direction _direction = Direction.vertical;
        
        public float? size
        {
            get => serializableSize;
            set => serializableSize = value;
        }
        
        [UxmlAttribute("size")]
        [CreateProperty]
        SerializableNullable<float> serializableSize
        {
            get => _serializableSize;
            set
            {
                if (_serializableSize == value)
                    return;

                _serializableSize = value;
                NotifyPropertyChanged(in sizeProperty);
                
                Update();
            }
        }
        SerializableNullable<float> _serializableSize = null;
        
        public float? maxSize
        {
            get => serializableMaxSize;
            set => serializableMaxSize = value;
        }
        
        [UxmlAttribute("max-size")]
        [CreateProperty]
        SerializableNullable<float> serializableMaxSize
        {
            get => _serializableMaxSize;
            set
            {
                if (_serializableMaxSize == value)
                    return;

                _serializableMaxSize = value;
                NotifyPropertyChanged(in maxSizeProperty);
                
                Update();
            }
        }
        SerializableNullable<float> _serializableMaxSize = null;
        
        
        [UxmlAttribute]
        [CreateProperty]
        public bool viewportSizeChange
        {
            get => _viewportSizeChange;
            set
            {
                if (_viewportSizeChange == value)
                    return;

                _viewportSizeChange = value;
                NotifyPropertyChanged(in viewportSizeChangeProperty);
                
                Update();
            }
        }
        bool _viewportSizeChange = false;

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

        public VisualElement? contentParent { get; }
        
        public VisualElement viewportClipping { get; }
        public VisualElement viewport { get; }
        public override VisualElement contentContainer { get; }



        readonly AnimBool animBool;
        readonly IVisualElementScheduledItem scheduledItem;
        
#if UNITY_EDITOR
        int inspectorEndingFrame = 0;
#endif

        public AnimatedFadedGroup() : this(false) { }
        public AnimatedFadedGroup(bool value, Direction direction = Direction.vertical, float? maxHeight = null) : this(value, null, new VisualElement { name = contentUssClassName }, direction, maxHeight) { }
        public AnimatedFadedGroup(VisualElement contentContainer, Direction direction = Direction.vertical, float? maxHeight = null) : this(false, null, contentContainer, direction, maxHeight) { }
        public AnimatedFadedGroup(VisualElement contentParent, VisualElement contentContainer, Direction direction = Direction.vertical, float? maxHeight = null) : this(false, contentParent, contentContainer, direction, maxHeight) { }
        public AnimatedFadedGroup(bool value, VisualElement contentContainer, Direction direction = Direction.vertical, float? maxHeight = null) : this(value, null, contentContainer, direction, maxHeight) { }
        public AnimatedFadedGroup(bool value, VisualElement? contentParent, VisualElement contentContainer, Direction direction = Direction.vertical, float? maxHeight = null)
        {
            _value = value;
            this.contentParent = contentParent;
            
            AddToClassList(ussClassName);
            
            this.RegisterDefaultStyleSheet(UIToolkitUtility.rosControlStyle);
            
            viewportClipping = new VisualElement { name = viewportClippingUssClassName };
            viewportClipping.AddToClassList(viewportClippingUssClassName);
            hierarchy.Add(viewportClipping);
            
            viewport = new VisualElement { name = viewportUssClassName };
            viewport.AddToClassList(viewportUssClassName);
            viewportClipping.hierarchy.Add(viewport);
            
            this.contentContainer = contentContainer;

            if (contentParent == null)
            {
                contentContainer.AddToClassList(contentUssClassName);
                viewport.hierarchy.Add(contentContainer);
            }

            _direction = direction;
            _serializableMaxSize = maxHeight;
            
            EnableInClassList(horizontalUssClassName, direction == Direction.horizontal);
            EnableInClassList(verticalUssClassName, direction == Direction.vertical);
            
            RegisterCallback<AttachToPanelEvent>(_ => Update());
            RegisterCallback<CustomStyleResolvedEvent>(CustomStyleResolvedEventCallback);
            contentContainer.RegisterCallback<GeometryChangedEvent>(_ => Update());
            
            animBool = new AnimBool(value);
            animBool.onAnimationBegin += OnAnimationBegin;
            scheduledItem = viewportClipping.schedule.Execute(Update).Every(0);
            scheduledItem.Pause();
            animBool.onAnimationEnd += OnAnimationEnd;
            
#if UNITY_EDITOR
            schedule.Execute(_ =>
            {
                if (inspectorEndingFrame >= 2)
                    return;
                
                inspectorEndingFrame++;
                
                // 레이아웃 갱신이 이미 애니메이션에 의해 이루어진 상태라 비활성화가 되어도 레이아웃이 갱신되지 않아 프리팹 바가 나타나는 버그를 수정합니다
                UnityEditor.UIElements.InspectorElement inspector = GetFirstAncestorOfType<UnityEditor.UIElements.InspectorElement>();
                if (inspector != null)
                {
                    GeometryChangedEvent evt = GeometryChangedEvent.GetPooled(inspector.layout, inspector.layout);
                    evt.target = inspector;

                    inspector.SendEvent(evt);
                }
            }).Every(0);
#endif
        }

        void OnAnimationBegin()
        {
            if (value)
                contentContainer.style.display = DisplayStyle.Flex;
            
            if (contentParent != null)
            {
                contentContainer.AddToClassList(contentUssClassName);
                viewport.hierarchy.Add(contentContainer);
            }

            scheduledItem.Resume();
        }

        void Update()
        {
            if (panel == null || float.IsNaN(contentContainer.resolvedStyle.width) || float.IsNaN(contentContainer.resolvedStyle.height))
                return;
            
            float width = (size ?? contentContainer.resolvedStyle.width)
                .Min(maxSize ?? float.MaxValue)
                .Min((contentParent?.resolvedStyle.maxWidth == StyleKeyword.Undefined ? (float?)contentParent.resolvedStyle.maxWidth.value : null) ?? float.MaxValue)
                * animBool.value;
            
            float height = (size ?? contentContainer.resolvedStyle.height)
                .Min(maxSize ?? float.MaxValue)
                .Min((contentParent?.resolvedStyle.maxHeight == StyleKeyword.Undefined ? (float?)contentParent.resolvedStyle.maxHeight.value : null) ?? float.MaxValue)
                * animBool.value;
            
            if (value)
                contentContainer.style.display = DisplayStyle.Flex;
            else if ((direction == Direction.horizontal && width <= 0) || (direction == Direction.vertical && height <= 0))
                contentContainer.style.display = DisplayStyle.None;

            // Null 값으로 설정해도 확률적으로 인라인 값이 돌아오지 않는 버그가 있어서 따로 처리
            viewportClipping.style.display = contentParent == null || animBool.isAnimating ? DisplayStyle.Flex : DisplayStyle.None;
            viewportClipping.style.maxHeight = maxSize != null ? maxSize.Value : StyleKeyword.Null;

            if (animBool.isAnimating && direction == Direction.horizontal)
            {
                viewportClipping.style.width = width;
                contentContainer.style.width = viewportSizeChange ? width : StyleKeyword.Null;
            }
            else
            {
                viewportClipping.style.width = StyleKeyword.Null;
                contentContainer.style.width = StyleKeyword.Null;
            }

            if (animBool.isAnimating && direction == Direction.vertical)
            {
                viewportClipping.style.height = height;
                contentContainer.style.height = viewportSizeChange ? height : StyleKeyword.Null;
            }
            else
            {
                viewportClipping.style.height = StyleKeyword.Null;
                contentContainer.style.height = StyleKeyword.Null;
            }
        }

        void OnAnimationEnd()
        {
            if (contentParent != null)
            {
                contentParent.hierarchy.Add(contentContainer);
                contentContainer.RemoveFromClassList(contentUssClassName);
            }

            scheduledItem.Pause();
            Update();

#if UNITY_EDITOR
            inspectorEndingFrame = 0;
#endif
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
            this.SetCheckedPseudoState(newValue);
            animBool.target = newValue;
        }
        
        public enum Direction
        {
            horizontal,
            vertical
        }
    }
}
