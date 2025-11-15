#nullable enable
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements;

[UxmlElement]
public partial class RectOffsetField : RuniBaseCompositeField<RectOffset>
{
    public new const string ussClassName = "runios-rect-offset-field";
    public new const string labelUssClassName = ussClassName + "__label";
    public new const string inputUssClassName = ussClassName + "__input";
        
    public RectOffsetField() : this(string.Empty) { }
    public RectOffsetField(string label) : base(label)
    {
        labelElement.AddToClassList(labelUssClassName);
        visualInput.AddToClassList(inputUssClassName);
            
        AddToClassList(ussClassName);
        SetFieldsByHorizontal();
    }

    protected override IEnumerable<IElementDescription> GetElementDescriptions()
    {
        yield return new FieldDescription<FloatField, float>
        (
            "L",
            nameof(RectOffset.left),
            static x => x.left,
            static (ref RectOffset offset, float fieldValue) => offset.left = fieldValue
        );
            
        yield return new FieldDescription<FloatField, float>
        (
            "R",
            nameof(RectOffset.right),
            static x => x.right,
            static (ref RectOffset offset, float fieldValue) => offset.right = fieldValue
        );

        yield return new FieldDescription<FloatField, float>
        (
            "T",
            nameof(RectOffset.top),
            static x => x.top,
            static (ref RectOffset offset, float fieldValue) => offset.top = fieldValue
        );
            
        yield return new FieldDescription<FloatField, float>
        (
            "B",
            nameof(RectOffset.bottom),
            static x => x.bottom,
            static (ref RectOffset offset, float fieldValue) => offset.bottom = fieldValue
        );
    }
}