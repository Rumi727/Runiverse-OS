#nullable enable
namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimFloat : BaseAnimValue<float>
    {
        public AnimFloat(float value) : base(value) { }
        
        public AnimFloat(float value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) {}
        
        public override float value => MathUtility.Lerp(start, target, progress.ClampToFloat());
    }
}