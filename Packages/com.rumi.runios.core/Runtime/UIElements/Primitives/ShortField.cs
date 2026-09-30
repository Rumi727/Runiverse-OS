#nullable enable
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Primitives
{
    [UxmlElement]
    public partial class ShortField : TextValueField<short>
    {
        public new const string ussClassName = "runios-short-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        
        public ShortField() : this(string.Empty) { }

        public ShortField(int maxLength) : this(string.Empty, maxLength) { }
        
        public ShortField(string label, int maxLength = 1000) : base(label, maxLength, new ShortInput())
        {
            AddToClassList(ussClassName);
            
            labelElement.AddToClassList(labelUssClassName);
            shortInput.AddToClassList(inputUssClassName);
            
            AddLabelDragger<short>();
        }
        
        ShortInput shortInput => (ShortInput)textInputBase;
        
        protected override string ValueToString(short value) => value.ToString(formatString, CultureInfo.InvariantCulture);

        protected override short StringToValue(string value)
        {
            if (short.TryParse(value, out short result))
                return result;
            else
                return rawValue;
        }

        public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, short startValue) => shortInput.ApplyInputDeviceDelta(delta, speed, startValue);

        protected class ShortInput : TextValueInput
        {
            public new ShortField parent => (ShortField)base.parent;

            protected override string allowedCharacters => "0123456789-";

            public override void ApplyInputDeviceDelta(Vector3 delta, DeltaSpeed speed, short startValue)
            {
                short dragSensitivity = NumericFieldDragUtility.CalculateSensitivity(startValue);
                int value = StringToValue(text) + (NumericFieldDragUtility.GetSignedDelta(delta, speed) * dragSensitivity).RoundToInt();

                if (parent.isDelayed)
                    text = ValueToString(value.ClampToShort());
                else
                    parent.value = value.ClampToShort();
            }

            protected override string ValueToString(short v) => v.ToString(formatString, CultureInfo.InvariantCulture.NumberFormat);

            protected override short StringToValue(string str) => parent.StringToValue(str);
        }
    }
}