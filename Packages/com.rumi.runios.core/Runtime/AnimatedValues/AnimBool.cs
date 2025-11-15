#nullable enable
namespace RuniOS.AnimatedValues
{
    [Serializable]
    public class AnimBool : BaseAnimValue<float>
    {
        public AnimBool(bool value) : base(value ? 1 : 0) { }
        
        public AnimBool(bool value, EasingFunction.Ease easing, double duration) : base(value ? 1 : 0, easing, duration) { }
        
        public new bool target
        {
            get => base.target > 0;
            set => base.target = value ? 1 : 0;
        }
        
        public override float value => MathUtility.Lerp(start.Clamp01(), base.target.Clamp01(), progress).ClampToFloat();

        public void SetValue(bool value) => base.SetValue(value ? 1 : 0);
    }
}