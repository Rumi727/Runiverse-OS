#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class ByteField : TextValueField<byte>
    {
        public new const string ussClassName = "runios-byte-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public ByteField() : this(string.Empty) { }

        public ByteField(int maxLength) : this(string.Empty, maxLength) { }

        public ByteField(string label, int maxLength = 1000) : base(label, maxLength, new ByteInput())
        {
            AddToClassList(ussClassName);

            labelElement.AddToClassList(labelUssClassName);
            byteInput.AddToClassList(inputUssClassName);

            AddLabelDragger<byte>();
        }

        ByteInput byteInput => (ByteInput)textInputBase;

        protected override string ValueToString(byte value) => value.ToString(formatString, CultureInfo.InvariantCulture);

        protected override byte StringToValue(string value)
        {
            if (byte.TryParse(value, out byte result))
                return result;
            else
                return rawValue;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, byte startValue) => byteInput.ApplyInputDeviceDelta(delta, speed, startValue);

        protected class ByteInput : TextValueInput
        {
            public new ByteField parent => (ByteField)base.parent;

            protected override string allowedCharacters => "0123456789-";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, byte startValue)
            {
                byte dragSensitivity = NumericFieldDragUtility.CalculateSensitivity(startValue);
                int value = StringToValue(text) + (NumericFieldDragUtility.GetSignedDelta(delta, speed) * dragSensitivity).RoundToInt();

                if (parent.isDelayed)
                    text = ValueToString(value.ClampToByte());
                else
                    parent.value = value.ClampToByte();
            }

            protected override string ValueToString(byte value) => parent.ValueToString(value);

            protected override byte StringToValue(string value) => parent.StringToValue(value);
        }
    }
}