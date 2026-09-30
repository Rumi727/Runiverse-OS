#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class DecimalField : TextValueField<decimal>
    {
        public new const string ussClassName = "runios-decimal-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";

        public DecimalField() : this(string.Empty) { }

        public DecimalField(int maxLength) : this(string.Empty, maxLength) { }

        public DecimalField(string label, int maxLength = 1000) : base(label, maxLength, new DecimalInput())
        {
            AddToClassList(ussClassName);

            labelElement.AddToClassList(labelUssClassName);
            textInputBase.AddToClassList(inputUssClassName);

            AddLabelDragger<decimal>();
        }

        DecimalInput decimalInput => (DecimalInput)textInputBase;

        protected override string ValueToString(decimal value) => value.ToString(formatString, CultureInfo.InvariantCulture);

        protected override decimal StringToValue(string value)
        {
            if (decimal.TryParse(value, out decimal result))
                return result;
            else
                return rawValue;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, decimal startValue) => decimalInput.ApplyInputDeviceDelta(delta, speed, startValue);

        protected class DecimalInput : TextValueInput
        {
            public new DecimalField parent => (DecimalField)base.parent;

            protected override string allowedCharacters => "0123456789.-";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, decimal startValue)
            {
                decimal dragSensitivity = NumericFieldDragUtility.CalculateSensitivity(startValue);
                decimal value = StringToValue(text) + ((decimal)NumericFieldDragUtility.GetSignedDelta(delta, speed) * dragSensitivity);

                if (parent.isDelayed)
                    text = ValueToString(value);
                else
                    parent.value = value;
            }

            protected override string ValueToString(decimal value) => parent.ValueToString(value);

            protected override decimal StringToValue(string value) => parent.StringToValue(value);
        }
    }
}