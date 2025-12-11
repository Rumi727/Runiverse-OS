#nullable enable
namespace RuniOS.Editor.Unity.Serialization.Converters
{
    [CustomPropertyConverter(typeof(AnimationCurve))]
    public class AnimationCurvePropertyConverter : PropertyConverter
    {
        public override object Read(SerializedProperty property, Type propertyType) => property.animationCurveValue;
        public override void Write(SerializedProperty property, Type propertyType, object? value) => property.animationCurveValue = (AnimationCurve)(value ?? new AnimationCurve());
    }
}