#nullable enable
namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimByte : BaseAnimValue<byte>
    {
        public AnimByte(byte value) : base(value) { }
        
        public AnimByte(byte value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

        public override byte value => MathUtility.Lerp(start, target, progress);
    }
}