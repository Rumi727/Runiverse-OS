#nullable enable
namespace RuniOS.AnimatedValues;

[Serializable]
public class AnimShort : BaseAnimValue<short>
{
    public AnimShort(short value) : base(value) { }
        
    public AnimShort(short value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

    public override short value => MathUtility.Lerp(start, target, progress);
}