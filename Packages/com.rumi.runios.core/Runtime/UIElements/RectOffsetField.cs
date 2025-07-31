#nullable enable
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    public class RectOffsetField : RuniBaseCompositeField<RectOffset>
    {
        public new const string ussClassName = "runios-type-field";
        
        public RectOffsetField() : this(null) { }
        public RectOffsetField(string? label) : base(label, 4) => AddToClassList(ussClassName);

        protected override IEnumerable<IElementDescription> DescribeFields()
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
}
