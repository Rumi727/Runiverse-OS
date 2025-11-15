#nullable enable
namespace RuniOS.AnimatedValues;

[Serializable]
public class AnimInt : BaseAnimValue<int>
{
    public AnimInt(int value) : base(value) { }
        
    public AnimInt(int value, EasingFunction.Ease easing, double duration) : base(value, easing, duration) { }

    public override int value => MathUtility.Lerp(start, target, progress);
}