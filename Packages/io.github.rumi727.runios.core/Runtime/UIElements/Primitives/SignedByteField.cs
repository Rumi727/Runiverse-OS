#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class SignedByteField : TextValueField<sbyte>
    {
        public new const string ussClassName = "runios-sbyte-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public SignedByteField() : this(string.Empty) { }

        public SignedByteField(int maxLength) : this(string.Empty, maxLength) { }

        public SignedByteField(string label, int maxLength = 1000) : base(label, maxLength, new SignedByteInput())
        {
            AddToClassList(ussClassName);

            labelElement.AddToClassList(labelUssClassName);
            sbyteInput.AddToClassList(inputUssClassName);

            AddLabelDragger<sbyte>();
        }

        SignedByteInput sbyteInput => (SignedByteInput)textInputBase;

        protected override string ValueToString(sbyte value) => value.ToString(formatString, CultureInfo.InvariantCulture);

        protected override sbyte StringToValue(string value)
        {
            if (sbyte.TryParse(value, out sbyte result))
                return result;
            else
                return rawValue;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, sbyte startValue) => sbyteInput.ApplyInputDeviceDelta(delta, speed, startValue);

        protected class SignedByteInput : TextValueInput
        {
            public new SignedByteField parent => (SignedByteField)base.parent;

            protected override string allowedCharacters => "0123456789-";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, sbyte startValue)
            {
                sbyte dragSensitivity = NumericFieldDragUtility.CalculateSensitivity(startValue);
                int value = StringToValue(text) + (NumericFieldDragUtility.GetSignedDelta(delta, speed) * dragSensitivity).RoundToInt();

                if (parent.isDelayed)
                    text = ValueToString(value.ClampToSByte());
                else
                    parent.value = value.ClampToSByte();
            }

            protected override string ValueToString(sbyte value) => parent.ValueToString(value);

            protected override sbyte StringToValue(string value) => parent.StringToValue(value);
        }
    }
}