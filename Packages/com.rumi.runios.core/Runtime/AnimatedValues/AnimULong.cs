#nullable enable
using System;

namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimULong : BaseAnimValue<ulong>
    {
        public AnimULong(ulong value) : base(value) { }
        
        public AnimULong(ulong value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

        public override ulong value => MathUtility.Lerp(start, target, progress);
    }
}