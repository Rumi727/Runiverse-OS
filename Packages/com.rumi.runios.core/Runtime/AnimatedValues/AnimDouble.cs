#nullable enable
namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimDouble : BaseAnimValue<double>
    {
        public AnimDouble(double value) : base(value) { }
        
        public AnimDouble(double value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) {}

        public override double value => MathUtility.Lerp(start, target, progress);
    }
}