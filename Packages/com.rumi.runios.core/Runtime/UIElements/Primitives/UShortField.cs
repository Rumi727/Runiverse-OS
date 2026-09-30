#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class UShortField : TextValueField<ushort>
    {
        public new const string ussClassName = "runios-ushort-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public UShortField() : this(string.Empty) { }

        public UShortField(int maxLength) : this(string.Empty, maxLength) { }

        public UShortField(string label, int maxLength = 1000) : base(label, maxLength, new UShortInput())
        {
            AddToClassList(ussClassName);

            labelElement.AddToClassList(labelUssClassName);
            ushortInput.AddToClassList(inputUssClassName);

            AddLabelDragger<ushort>();
        }

        UShortInput ushortInput => (UShortInput)textInputBase;

        protected override string ValueToString(ushort value) => value.ToString(formatString, CultureInfo.InvariantCulture);

        protected override ushort StringToValue(string value)
        {
            if (ushort.TryParse(value, out ushort result))
                return result;
            else
                return rawValue;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, ushort startValue) => ushortInput.ApplyInputDeviceDelta(delta, speed, startValue);

        protected class UShortInput : TextValueInput
        {
            public new UShortField parent => (UShortField)base.parent;

            protected override string allowedCharacters => "0123456789";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, ushort startValue)
            {
                ushort dragSensitivity = NumericFieldDragUtility.CalculateSensitivity(startValue);
                int value = StringToValue(text) + (NumericFieldDragUtility.GetSignedDelta(delta, speed) * dragSensitivity).RoundToInt();

                if (parent.isDelayed)
                    text = ValueToString(value.ClampToUShort());
                else
                    parent.value = value.ClampToUShort();
            }

            protected override string ValueToString(ushort value) => parent.ValueToString(value);

            protected override ushort StringToValue(string value) => parent.StringToValue(value);
        }
    }
}