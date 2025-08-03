#nullable enable
using System;

namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimSByte : BaseAnimValue<sbyte>
    {
        public AnimSByte(sbyte value) : base(value) { }
        
        public AnimSByte(sbyte value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

        public override sbyte value => MathUtility.Lerp(start, target, progress);
    }
}