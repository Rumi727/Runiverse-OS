#nullable enable
using System;

namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimBool : AnimFloat
    {
        public AnimBool(bool value) : base(value ? 1 : 0) { }
        
        public AnimBool(bool value, EasingFunction.Ease easing, double duration) : base(value ? 1 : 0, easing, duration) { }
        
        public new bool target
        {
            get => base.target > 0;
            set => base.target = value ? 1 : 0;
        }
    }
}