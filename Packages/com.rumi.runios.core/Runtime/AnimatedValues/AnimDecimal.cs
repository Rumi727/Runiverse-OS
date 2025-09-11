#nullable enable
using System;

namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimDecimal : BaseAnimValue<decimal>
    {
        public AnimDecimal(decimal value) : base(value) { }
        
        public AnimDecimal(decimal value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) {}

        public override decimal value => MathUtility.Lerp(start.ClampToDouble(), target.ClampToDouble(), progress).ClampToDecimal();
    }
}