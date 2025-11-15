#nullable enable
namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimUShort : BaseAnimValue<ushort>
    {
        public AnimUShort(ushort value) : base(value) { }
        
        public AnimUShort(ushort value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

        public override ushort value => MathUtility.Lerp(start, target, progress);
    }
}