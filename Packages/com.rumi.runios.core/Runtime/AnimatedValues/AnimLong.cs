#nullable enable
using System;

namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimLong : BaseAnimValue<long>
    {
        public AnimLong(long value) : base(value) { }
        
        public AnimLong(long value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

        public override long value => MathUtility.Lerp(start, target, progress);
    }
}