#nullable enable
using RuniOS.LowLevel;

namespace RuniOS.AnimatedValues
{
    [Serializable]
    public abstract class BaseAnimValue<T> : ISerializationCallbackReceiver
    {
        protected BaseAnimValue(T value)
        {
            _start = value;
            _target = value;

            _currentTime = duration.Clamp(0);
        }
        
        protected BaseAnimValue(T value, EasingFunction.Ease easing, double duration) : this(value)
        {
            this.easing = easing;
            this.duration = duration;
        }

        public bool isAnimating => _isAnimating;
        [NonSerialized] bool _isAnimating;

        public event Action? onAnimationBegin;
        public event Action? onAnimationEnd;

        public T start { get => _start; set => _start = value; }
        [SerializeField] T _start;
        
        public abstract T value { get; }

        public T target
        {
            get => _target;
            set
            {
                if (EqualityComparer<T>.Default.Equals(target, value))
                    return;
                
                start = this.value;
                _target = value;

                _currentTime = 0;
                BeginAnimating();
            }
        }
        [SerializeField] T _target;

        public double currentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value.Clamp(0);
                if (EqualityComparer<T>.Default.Equals(this.value, target) || currentTime >= duration)
                    StopAnimating();
            }
        }
        [SerializeField] double _currentTime = 0;

        public double duration { get => _duration; set => _duration = value; }
        [SerializeField] double _duration = 0.5;

        public double progress
        {
            get
            {
                if (duration > 0)
                    return isAnimating ? EasingFunction.EasingCalculate(0, 1, (currentTime / duration).Clamp01(), easing) : 1;
                else
                    return 1;
            }
            set
            {
                currentTime = (value * duration).Clamp(0, duration);
                BeginAnimating();
            }
        }
        
        public EasingFunction.Ease easing
        {
            get => _easing;
            set => _easing = value;
        }
        [SerializeField] EasingFunction.Ease _easing = EasingFunction.Ease.EaseOutQuart;

        void BeginAnimating()
        {
            if (isAnimating)
                return;
            
            _isAnimating = true;
            onAnimationBegin?.Invoke();
            
            if (duration > 0)
            {
#if UNITY_EDITOR
                if (Kernel.isPlaying)
                    RuniPlayerLoop.onPostLateUpdate += Update;
                else
                    UnityEditor.EditorApplication.update += Update;
#else
                RuniPlayerLoop.onPreLateUpdate += Update;
#endif
            }
            else
                StopAnimating();
        }

        void Update() => currentTime += Kernel.deltaTimeDouble;

        public void StopAnimating()
        {
            if (!isAnimating)
                return;

            if (duration > 0)
            {
#if UNITY_EDITOR
                if (Kernel.isPlaying)
                    RuniPlayerLoop.onPostLateUpdate -= Update;
                else
                    UnityEditor.EditorApplication.update -= Update;
#else
                RuniPlayerLoop.onPreLateUpdate -= Update;
#endif
            }

            _isAnimating = false;
            currentTime = duration.Clamp(0);
            
            onAnimationEnd?.Invoke();
        }

        public void SetValue(T value)
        {
            StopAnimating();
            
            _start = value;
            _target = value;
        }
        
        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            if (duration > 0 && currentTime < duration)
                BeginAnimating();
        }
    }
}