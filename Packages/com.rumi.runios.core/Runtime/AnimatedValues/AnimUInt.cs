#nullable enable
namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimUInt : BaseAnimValue<uint>
    {
        public AnimUInt(uint value) : base(value) { }
        
        public AnimUInt(uint value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

        public override uint value => MathUtility.Lerp(start, target, progress);
    }
}